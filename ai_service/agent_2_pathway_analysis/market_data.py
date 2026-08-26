"""
Allow-listed tool: get_market_data(search_term)

Fetches real job-market demand/competition/trend signals for a career title
using the Adzuna Jobs API (free tier, no-cost, https://developer.adzuna.com/).

Design notes (relevant to the assignment's tool-safety requirements):
- This is the ONLY external call this agent is permitted to make (least privilege).
- Every input is a short, validated plain-text job title pulled from our own
  knowledge base (never raw user text), so there is no prompt-injection surface here.
- One retry (with a short backoff) is attempted on a transient network/HTTP
  failure before giving up on the live call.
- Network failures, timeouts, missing API keys, or empty results all degrade
  SAFELY to a deterministic simulated dataset rather than crashing the workflow.
  The response always discloses which happened via `data_source`.
- Trend is computed by comparing a RECENT posting window against a longer
  BASELINE window (two calls, same allow-listed endpoint) rather than assumed.
"""

import os
import time
import hashlib
from typing import Optional
import httpx

from .schemas import MarketData

ADZUNA_APP_ID = os.getenv("ADZUNA_APP_ID", "")
ADZUNA_APP_KEY = os.getenv("ADZUNA_APP_KEY", "")
ADZUNA_COUNTRY = os.getenv("ADZUNA_COUNTRY", "gb")  # Adzuna has no 'lk' coverage; default to a supported region
ADZUNA_TIMEOUT_SECONDS = 5.0
MAX_RETRIES = 1
RETRY_BACKOFF_SECONDS = 0.5

RECENT_WINDOW_DAYS = 14
BASELINE_WINDOW_DAYS = 90

_BASE_URL = "https://api.adzuna.com/v1/api/jobs/{country}/search/1"


def _simulated_market_data(search_term: str) -> MarketData:
    """
    Deterministic offline fallback. Uses a hash of the search term so results
    are stable across repeated calls/tests (not random), while still varying
    per pathway. Clearly labeled so it is never mistaken for live data.
    """
    seed = int(hashlib.sha256(search_term.encode()).hexdigest(), 16)
    demand_score = 40 + (seed % 55)              # 40-94
    competition_score = 30 + ((seed // 7) % 60)  # 30-89
    sample_size = 50 + (seed % 450)              # 50-499

    trend_bucket = (seed // 13) % 3
    trend = ["rising", "stable", "declining"][trend_bucket]

    return MarketData(
        demand_score=demand_score,
        competition_score=competition_score,
        trend=trend,
        sample_size=sample_size,
        data_source="simulated_fallback",
        region=ADZUNA_COUNTRY,
    )


def _adzuna_search(search_term: str, max_days_old: int) -> dict:
    """One validated call to the allow-listed Adzuna search endpoint, with one retry."""
    url = _BASE_URL.format(country=ADZUNA_COUNTRY)
    params = {
        "app_id": ADZUNA_APP_ID,
        "app_key": ADZUNA_APP_KEY,
        "what": search_term,
        "max_days_old": max_days_old,
        "results_per_page": 50,
        "content-type": "application/json",
    }

    last_error: Optional[Exception] = None
    for attempt in range(MAX_RETRIES + 1):
        try:
            with httpx.Client(timeout=ADZUNA_TIMEOUT_SECONDS) as client:
                resp = client.get(url, params=params)
            if resp.status_code == 200:
                return resp.json()
            last_error = ValueError(f"Adzuna returned status {resp.status_code}")
        except (httpx.TimeoutException, httpx.HTTPError) as e:
            last_error = e

        if attempt < MAX_RETRIES:
            time.sleep(RETRY_BACKOFF_SECONDS)

    raise last_error or RuntimeError("Adzuna call failed for an unknown reason")


def get_market_data(search_term: str) -> MarketData:
    """
    Validated tool entrypoint. `search_term` must be a short plain-text job
    title (already validated/whitelisted by the caller from knowledge_base.py).
    """
    if not search_term or len(search_term) > 80:
        return _simulated_market_data(search_term or "unknown")

    if not ADZUNA_APP_ID or not ADZUNA_APP_KEY:
        return _simulated_market_data(search_term)

    try:
        baseline_data = _adzuna_search(search_term, max_days_old=BASELINE_WINDOW_DAYS)
        recent_data = _adzuna_search(search_term, max_days_old=RECENT_WINDOW_DAYS)

        baseline_count: Optional[int] = baseline_data.get("count")
        recent_count: Optional[int] = recent_data.get("count")
        results = baseline_data.get("results", [])

        if not baseline_count or baseline_count <= 0:
            return _simulated_market_data(search_term)

        # Normalize raw posting count into a bounded 0-100 demand score.
        demand_score = min(100, int(baseline_count / 20))
        # Competition proxy: fewer distinct companies per posting ~ more competitive niche.
        distinct_companies = len({r.get("company", {}).get("display_name", "") for r in results}) or 1
        competition_score = max(5, min(100, 100 - int((distinct_companies / max(len(results), 1)) * 100)))

        # Trend: compare postings-per-day in the recent window vs the baseline window.
        recent_rate = (recent_count or 0) / RECENT_WINDOW_DAYS
        baseline_rate = baseline_count / BASELINE_WINDOW_DAYS
        if baseline_rate <= 0:
            trend = "stable"
        elif recent_rate > baseline_rate * 1.15:
            trend = "rising"
        elif recent_rate < baseline_rate * 0.85:
            trend = "declining"
        else:
            trend = "stable"

        return MarketData(
            demand_score=demand_score,
            competition_score=competition_score,
            trend=trend,
            sample_size=min(baseline_count, len(results) if results else baseline_count),
            data_source="adzuna_live",
            region=ADZUNA_COUNTRY,
        )
    except (httpx.TimeoutException, httpx.HTTPError, ValueError, KeyError) as e:
        print(f"[Agent2 market_data] Adzuna call failed after retry, using fallback: {e}")
        return _simulated_market_data(search_term)
