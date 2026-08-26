# Agent 2 — Career Discovery: Architecture

Agent 2 takes a student's completed profile (produced by Agent 1) and returns
three ranked, market-checked, human-reviewable career pathway recommendations.
This document explains the graph, the full request path across all three
services, the security guardrails, and how the design maps to the assignment's
backend/tool-safety requirements.

## 1. End-to-end request path

```
React (CareerDiscoveryPage)
   │  POST /api/career-discovery/analyze   (JWT bearer token)
   ▼
ASP.NET Core — CareerDiscoveryController
   │  1. Loads the caller's StudentProfile from PostgreSQL (never trusts the client for this)
   │  2. Calls IAgentService.ProcessAgent2AnalysisAsync(profile)
   ▼
   │  POST http://localhost:8000/api/v1/agent-2/analyze   (internal call only)
   ▼
Python FastAPI + LangGraph — agent_2_pathway_analysis
   │  Runs the 6-node graph below, returns Agent2AnalysisResponse
   ▲
ASP.NET Core — persists the result as a PathwayAnalysis row
(status = pending_approval), returns it to React with a DB id
   │
React — renders Path A/B/C, exposes Approve/Reject
   │  PATCH /api/career-discovery/{id}/approve | /reject
   ▼
ASP.NET Core — CareerDiscoveryController.SetStatusAsync
   updates the same row's Status/ApprovedAt/ApprovedByUserId
```

**Why this shape matters:** the Python microservice is never called directly
by React or Flutter — only the ASP.NET Core backend calls it (`AgentService`
via `HttpClient`, base URL from `AiServiceSettings:BaseUrl`). This satisfies
the backend rule that the Python service must operate as an internal service
behind the .NET API gateway, and it's also what lets the human-approval
decision be persisted durably in PostgreSQL rather than living only in the
agent's response.

## 2. The 6-node LangGraph flow

```
plan → match_candidates → fetch_market_data → generate_reasoning → validate → finalize
```

| # | Node | What it does | LLM involved? |
|---|------|---------------|----------------|
| 1 | `plan` | Records an explicit step list on the state, purely for auditability. | No |
| 2 | `match_candidates` | Deterministic weighted-overlap scoring against the allow-listed `CAREER_PATHWAYS` knowledge base (skills 45%, interests 30%, ambition-text match 25%). Also computes `missing_skills` as a plain set difference. Keeps the top 3. | No |
| 3 | `fetch_market_data` | One validated tool call per candidate to Adzuna (`get_market_data`), fetching demand/competition/trend. Falls back to a deterministic simulated dataset if no API key, a timeout, or an empty result — always labelled via `data_source`. | No |
| 4 | `generate_reasoning` | Asks the LLM to write a short explanation per candidate from the **already-fixed** candidate list. If no LLM key, or the call/parse fails, falls back to a template built from the matched skills/interests. | Yes (optional) |
| 5 | `validate` | Deterministic guardrails: rejects any recommendation naming a pathway outside the matched set (hallucination), duplicates, out-of-range scores, an invalid `trend` literal, a `roadmap` that doesn't line up with `recommended_courses`, and — critically — a `missing_skills` list that differs from what step 2 computed (so the LLM cannot quietly rewrite the skill gap). | No |
| 6 | `finalize` | Sets `status = "pending_approval"` if validation passed, else `"failed"`. `pending_approval` is the deliberate pause for a human (advisor/student) to approve or reject before anything is treated as official. | No |

Every node is wrapped by `_traced()` in `agent.py`, which records
`{node, duration_ms, status}` into `execution_trace` on the response —
this is the observability evidence referenced below.

## 3. Security guardrails

- **Allow-listed knowledge base.** `CAREER_PATHWAYS` is static, local data —
  the agent never scrapes or reads arbitrary web content, so there's no
  retrieval-based prompt-injection surface.
- **Least-privilege tool.** `get_market_data()` is the only external call
  Agent 2 is permitted to make. Its only input is a short, pre-validated job
  title pulled from the knowledge base — never raw user text.
- **Prompt-injection guard.** The reasoning prompt (`prompts.py`) explicitly
  tells the LLM that the STUDENT PROFILE block — especially the free-text
  `career_ambitions` field the student typed themselves — is data to
  reference, never instructions to obey, even if it contains phrases that
  look like commands.
- **Hallucination guard.** The `validate` node is deterministic code, not
  another LLM call, so it can't be talked out of enforcing its rules.
- **Safe-failure wrapper.** `run_agent_2_analysis()` wraps the entire graph
  invocation in `try/except`. An unexpected exception (LLM outage, malformed
  data, etc.) never surfaces as a raw 500 — it's converted into a structured
  `status="failed"` response with a `validation_errors` entry describing
  what happened.
- **Human-in-the-loop.** No recommendation is ever auto-applied to a
  student's profile. `pending_approval` is a hard stop until a
  `PATCH /{id}/approve` or `/reject` call is made by an authenticated user
  who owns that profile.

## 4. Persistence (PostgreSQL)

`PathwayAnalysis` (see `backend/PathwayNavigator.Api/Models/PathwayAnalysis.cs`):

| Column | Purpose |
|---|---|
| `Id` | Primary key, returned to the client so it can later approve/reject. |
| `StudentProfileId` | FK to the owning student profile. |
| `WorkflowId` | The Python graph run's UUID, for cross-referencing logs. |
| `RecommendationsJson` | The 3 recommendations, serialized as-is from the agent. |
| `Status` | `pending_approval` → `approved` \| `rejected` (or `failed` if validation didn't pass). |
| `CreatedAt` / `ApprovedAt` / `ApprovedByUserId` | Audit trail for the human decision. |

`CareerDiscoveryController` enforces that approve/reject can only transition
a row that is still `pending_approval` (a second attempt returns `409
Conflict`), and every read/write is scoped to the calling user's own
`StudentProfile` — one user cannot see or decide another's analysis.

## 5. Observability

Every `Agent2AnalysisResponse` includes `execution_trace`, e.g.:

```json
"execution_trace": [
  {"node": "plan", "duration_ms": 0.0, "status": "success"},
  {"node": "match_candidates", "duration_ms": 0.18, "status": "success"},
  {"node": "fetch_market_data", "duration_ms": 0.16, "status": "success"},
  {"node": "generate_reasoning", "duration_ms": 1694.38, "status": "success"},
  {"node": "validate", "duration_ms": 0.04, "status": "success"},
  {"node": "finalize", "duration_ms": 0.0, "status": "success"}
]
```

This makes each run's timing and outcome visible without any external
tracing tooling — useful both for debugging and as viva evidence that the
6-node graph actually executed as described above.

## 6. Testing

- `ai_service/tests/test_agent_2.py` (pytest, offline/deterministic): scoring
  math, `missing_skills`, the market-data fallback, trend calculation from
  mocked posting rates, the validation hallucination guard, and one
  golden-case end-to-end run asserting `validation_errors == []`.
- `backend/PathwayNavigator.Tests/` (xUnit): `PathwayAnalysisService` against
  an EF Core InMemory database (create/persist, ownership scoping, the
  pending-only approval guard), and `CareerDiscoveryController` with mocked
  dependencies covering every HTTP outcome (400/502/200/404/409).
