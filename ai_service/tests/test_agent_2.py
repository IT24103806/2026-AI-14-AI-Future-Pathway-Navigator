"""
Pytest coverage for Agent 2 (Career Discovery): deterministic scoring,
the market-data tool's offline fallback and trend calculation, the
validation node's hallucination guard, and one golden-case end-to-end run.

Run from ai_service/: `pytest` (pythonpath is set via pytest.ini).
"""
import pytest

from agent_2_pathway_analysis import agent as agent_module
from agent_2_pathway_analysis import market_data as market_data_module
from agent_2_pathway_analysis.agent import _score_pathway, run_agent_2_analysis, validation_node
from agent_2_pathway_analysis.knowledge_base import CAREER_PATHWAYS
from agent_2_pathway_analysis.market_data import get_market_data
from agent_2_pathway_analysis.schemas import (
    CareerCandidate,
    CareerPathRecommendation,
    StudentProfileInput,
)

AI_ML_PATHWAY = next(p for p in CAREER_PATHWAYS if p["pathway_name"] == "AI / Machine Learning Engineer")


# ----------------------------- _score_pathway() -----------------------------

def test_score_pathway_computes_weighted_overlap_and_missing_skills():
    profile = StudentProfileInput(
        academic_stage="Undergraduate",
        core_skills=["python", "machine learning"],
        hobbies_interests=["ai"],
        career_ambitions="I want to be an AI Engineer",
    )

    score, matched_skills, matched_interests, missing_skills = _score_pathway(AI_ML_PATHWAY, profile)

    # skill_score = 2/5 * 0.45 = 0.18, interest_score = 1/5 * 0.30 = 0.06,
    # ambition_score = 1.0 * 0.25 = 0.25 ("ai engineer" is a substring of the ambition text)
    # total = 0.49 -> 49
    assert score == 49
    assert matched_skills == ["machine learning", "python"]
    assert matched_interests == ["ai"]
    assert missing_skills == ["deep learning", "problem solving", "sql"]


def test_score_pathway_zero_overlap_scores_zero():
    profile = StudentProfileInput(
        academic_stage="Undergraduate",
        core_skills=["figma", "graphic design"],
        hobbies_interests=["drawing"],
        career_ambitions="I want to work in fashion",
    )

    score, matched_skills, matched_interests, _ = _score_pathway(AI_ML_PATHWAY, profile)

    assert score == 0
    assert matched_skills == []
    assert matched_interests == []


# ----------------------------- market_data fallback -----------------------------

def test_market_data_falls_back_to_simulated_when_no_api_key(monkeypatch):
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_ID", "")
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_KEY", "")

    result = get_market_data("machine learning engineer")

    assert result.data_source == "simulated_fallback"
    assert result.trend in ("rising", "stable", "declining")
    assert 0 <= result.demand_score <= 100
    assert 0 <= result.competition_score <= 100


def test_market_data_simulated_fallback_is_deterministic(monkeypatch):
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_ID", "")
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_KEY", "")

    first = get_market_data("data scientist")
    second = get_market_data("data scientist")

    assert first.demand_score == second.demand_score
    assert first.competition_score == second.competition_score
    assert first.trend == second.trend


# ----------------------------- trend calculation -----------------------------

def _adzuna_response(count, num_companies=5):
    return {
        "count": count,
        "results": [{"company": {"display_name": f"Company {i}"}} for i in range(num_companies)],
    }


@pytest.mark.parametrize(
    "baseline_count,recent_count,expected_trend",
    [
        (900, 200, "rising"),      # baseline_rate=10/day, recent_rate≈14.3/day (>1.15x)
        (900, 50, "declining"),    # recent_rate≈3.6/day (<0.85x)
        (900, 140, "stable"),      # recent_rate=10/day (within +/-15%)
    ],
)
def test_market_data_trend_from_live_posting_rates(monkeypatch, baseline_count, recent_count, expected_trend):
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_ID", "dummy-id")
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_KEY", "dummy-key")

    def fake_adzuna_search(search_term, max_days_old):
        if max_days_old == market_data_module.BASELINE_WINDOW_DAYS:
            return _adzuna_response(baseline_count)
        return _adzuna_response(recent_count)

    monkeypatch.setattr(market_data_module, "_adzuna_search", fake_adzuna_search)

    result = get_market_data("machine learning engineer")

    assert result.data_source == "adzuna_live"
    assert result.trend == expected_trend


# ----------------------------- validation_node hallucination guard -----------------------------

def test_validation_node_catches_hallucinated_pathway():
    candidates = [
        CareerCandidate(
            pathway_name="AI / Machine Learning Engineer",
            match_score=80,
            matched_skills=["python"],
            matched_interests=["ai"],
            missing_skills=["sql"],
            search_term="machine learning engineer",
        )
    ]
    recommendations = [
        CareerPathRecommendation(
            label="Path A",
            pathway_name="Totally Fabricated Pathway",  # not in candidates -> hallucination
            match_score=80,
            demand_score=50,
            competition_score=50,
            trend="stable",
            data_source="simulated_fallback",
            reasoning="This pathway does not exist in the knowledge base at all.",
            missing_skills=["sql"],
            recommended_courses=["Some Course"],
            roadmap=[{"phase": "Foundation", "course": "Some Course"}],
        )
    ]

    result = validation_node({"candidates": candidates, "recommendations": recommendations})

    assert result["is_valid"] is False
    assert any("hallucination" in err for err in result["validation_errors"])


def test_validation_node_passes_clean_recommendations():
    candidates = [
        CareerCandidate(
            pathway_name="AI / Machine Learning Engineer",
            match_score=80,
            matched_skills=["python"],
            matched_interests=["ai"],
            missing_skills=["sql"],
            search_term="machine learning engineer",
        )
    ]
    recommendations = [
        CareerPathRecommendation(
            label="Path A",
            pathway_name="AI / Machine Learning Engineer",
            match_score=80,
            demand_score=50,
            competition_score=50,
            trend="stable",
            data_source="simulated_fallback",
            reasoning="This is a valid, sufficiently long reasoning string.",
            missing_skills=["sql"],
            recommended_courses=["Some Course"],
            roadmap=[{"phase": "Foundation", "course": "Some Course"}],
        )
    ]

    result = validation_node({"candidates": candidates, "recommendations": recommendations})

    assert result["is_valid"] is True
    assert result["validation_errors"] == []


# ----------------------------- golden-case end-to-end -----------------------------

def test_golden_case_full_workflow_produces_valid_response(monkeypatch):
    # Force the deterministic fallback path (no live LLM/Adzuna calls) so this
    # test is fast, offline, and reproducible.
    monkeypatch.setattr(agent_module, "get_llm", lambda: None)
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_ID", "")
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_KEY", "")

    profile = StudentProfileInput(
        academic_stage="Undergraduate",
        core_skills=["python", "sql", "machine learning", "problem solving"],
        hobbies_interests=["ai", "data analysis"],
        career_ambitions="I want to become an AI engineer",
    )

    response = run_agent_2_analysis(profile)

    assert response.status == "pending_approval"
    assert response.validation_errors == []
    assert len(response.recommendations) == 3
    assert all(rec.reasoning for rec in response.recommendations)
    assert all(rec.day_in_the_life for rec in response.recommendations)
    assert all(rec.salary_range_lkr for rec in response.recommendations)
    assert all(len(rec.industry_tools) > 0 for rec in response.recommendations)
    assert all(len(rec.portfolio_projects) >= 3 for rec in response.recommendations)
    assert all(len(rec.recommended_certifications) >= 2 for rec in response.recommendations)
    assert all(len(rec.sri_lankan_education_routes) >= 3 for rec in response.recommendations)

    # Observability: every node ran and reported success.
    assert len(response.execution_trace) == 6
    assert all(entry.status == "success" for entry in response.execution_trace)


def test_every_pathway_in_knowledge_base_has_enriched_deep_dive_fields():
    required_keys = {
        "day_in_the_life",
        "salary_range_lkr",
        "industry_tools",
        "portfolio_projects",
        "recommended_certifications",
        "sri_lankan_education_routes",
    }
    for pathway in CAREER_PATHWAYS:
        assert required_keys <= set(pathway.keys()), pathway["pathway_name"]
        assert len(pathway["day_in_the_life"]) > 20
        assert "LKR" in pathway["salary_range_lkr"]
        assert len(pathway["industry_tools"]) >= 4
        assert len(pathway["portfolio_projects"]) >= 3
        assert len(pathway["recommended_certifications"]) >= 2
        assert len(pathway["sri_lankan_education_routes"]) >= 3

