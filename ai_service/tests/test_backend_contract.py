"""
Integration-contract tests between the ASP.NET Core gateway and this AI service.

The .NET backend (`backend/PathwayNavigator.Api/Services/AgentService.cs`) is the only
caller of these endpoints.  The tests below:

* send exactly the JSON payload shapes that AgentService builds, and
* check that every field the C# DTOs read (via `[JsonPropertyName("...")]`) is present in
  the real FastAPI response, so a rename on either side is caught in CI.

Runs fully offline: LLM and market-data providers are disabled.
"""
import re
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from agent_2_pathway_analysis import market_data as market_data_module
from agent_2_pathway_analysis.knowledge_base import CAREER_PATHWAYS
from main import app

DTO_ROOT = Path(__file__).resolve().parents[2] / "backend" / "PathwayNavigator.Api" / "DTOs"
API = "/api/v1"


@pytest.fixture(autouse=True)
def offline(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_ID", "")
    monkeypatch.setattr(market_data_module, "ADZUNA_APP_KEY", "")


@pytest.fixture(scope="module")
def client():
    return TestClient(app)


def dto_fields(relative_file: str, class_name: str) -> set[str]:
    """JSON property names declared on a C# DTO class."""
    source = (DTO_ROOT / relative_file).read_text(encoding="utf-8")
    match = re.search(r"class\s+" + class_name + r"\b(.*?)\n\s{4}\}\n|class\s+" + class_name + r"\b(.*?)\n\}\n", source, re.S)
    assert match, f"{class_name} not found in {relative_file}"
    return set(re.findall(r'JsonPropertyName\("([^"]+)"\)', match.group(0)))


def assert_dto_satisfied(payload: dict, relative_file: str, class_name: str, backend_populated: frozenset = frozenset()):
    expected = dto_fields(relative_file, class_name) - set(backend_populated)
    assert expected, f"No JsonPropertyName fields parsed for {class_name}"
    assert expected <= set(payload), f"{class_name} expects {sorted(expected - set(payload))} from the AI service"


def test_agent_1_chat_matches_AgentChatResponseDto(client):
    payload = {
        "user_id": "00000000-0000-0000-0000-000000000001",
        "message": "I am an undergraduate at SLIIT",
        "history": [{"role": "assistant", "content": "Hello!"}],
        "current_slots": {"academic_stage": None, "core_skills": [], "hobbies_interests": [], "career_ambitions": None},
    }
    response = client.post(f"{API}/agent-1/chat", json=payload)
    assert response.status_code == 200
    body = response.json()
    # `is_saved` is set by the .NET OnboardingController after persisting, not by Python.
    assert_dto_satisfied(body, "Onboarding/AgentChatResponseDto.cs", "AgentChatResponseDto", frozenset({"is_saved"}))
    assert_dto_satisfied(body["extracted_slots"], "Onboarding/AgentChatRequestDto.cs", "ExtractedSlotsDto")


def test_agent_1_accepts_null_current_slots(client):
    response = client.post(f"{API}/agent-1/chat", json={"user_id": None, "message": "hi", "history": [], "current_slots": None})
    assert response.status_code == 200


def test_agent_2_analysis_matches_PathwayAnalysisResponseDto(client):
    payload = {
        "user_id": "u1",
        "profile": {
            "academic_stage": "Undergraduate", "core_skills": ["Python", "SQL"],
            "hobbies_interests": ["AI"], "career_ambitions": "AI Engineer",
        },
    }
    response = client.post(f"{API}/agent-2/analyze", json=payload)
    assert response.status_code == 200
    body = response.json()
    # `id` is assigned by the .NET PathwayAnalysisService once the row is persisted.
    assert_dto_satisfied(body, "Pathway/PathwayAnalysisResponseDto.cs", "PathwayAnalysisResponseDto", frozenset({"id"}))
    assert body["status"] == "pending_approval" and body["recommendations"]
    assert_dto_satisfied(body["recommendations"][0], "Pathway/PathwayAnalysisResponseDto.cs", "CareerPathRecommendationDto")
    assert_dto_satisfied(body["recommendations"][0]["roadmap"][0], "Pathway/PathwayAnalysisResponseDto.cs", "RoadmapStepDto")
    assert_dto_satisfied(body["execution_trace"][0], "Pathway/PathwayAnalysisResponseDto.cs", "ExecutionTraceEntryDto")


def test_agent_3_plan_matches_PathwayPlannerResponseDto(client):
    payload = {
        "user_id": "u1", "selected_pathway": CAREER_PATHWAYS[0]["pathway_name"],
        "profile": {"academic_stage": "Undergraduate", "core_skills": ["Python"], "career_ambitions": "AI Engineer"},
        "completed_phases": [],
    }
    response = client.post(f"{API}/agent-3/plan", json=payload)
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ready"
    assert_dto_satisfied(body, "Pathway/PathwayPlannerDtos.cs", "PathwayPlannerResponseDto")
    assert_dto_satisfied(body["roadmap"][0], "Pathway/PathwayPlannerDtos.cs", "RoadmapStageDto")


def test_agent_4_evaluate_matches_RealityCheckAgentResponseDto(client):
    payload = {
        "student_id": "11111111-1111-1111-1111-111111111111",
        "pathway_analysis_id": "22222222-2222-2222-2222-222222222222",
        "target_career": "Software Engineer", "al_stream": "Physical Science",
        "al_results": "A,B,C", "budget_level": "Medium", "current_skills": ["Python"],
    }
    response = client.post(f"{API}/agent-4/evaluate", json=payload)
    assert response.status_code == 200
    body = response.json()
    assert_dto_satisfied(body, "Review/RealityCheckDto.cs", "RealityCheckAgentResponseDto")
    assert_dto_satisfied(body["tool_calls"][0], "Review/RealityCheckDto.cs", "AgentToolCallDto")
    assert_dto_satisfied(body["execution_trace"][0], "Review/RealityCheckDto.cs", "AgentExecutionStepDto")


@pytest.mark.parametrize("pathway", [p["pathway_name"] for p in CAREER_PATHWAYS])
def test_every_agent_2_pathway_is_known_to_agent_4(client, pathway):
    """Career Discovery -> Reality Check hand-off: a recommended pathway must never be
    flagged as 'absent from the verified catalogue' just because the names differ."""
    payload = {
        "student_id": "student-1", "pathway_analysis_id": "analysis-1", "target_career": pathway,
        "al_stream": "Physical Science", "al_results": "A,B,C", "budget_level": "Medium", "current_skills": [],
    }
    body = client.post(f"{API}/agent-4/evaluate", json=payload).json()
    assert "none" not in body["tool_calls"][0]["result_summary"], body["tool_calls"][0]["result_summary"]
    assert not (body["risk_reason"] or "").startswith("Evidence risk")


def test_agent_3_accepts_every_agent_2_pathway(client):
    for pathway in CAREER_PATHWAYS:
        body = client.post(f"{API}/agent-3/plan", json={"selected_pathway": pathway["pathway_name"], "profile": {}}).json()
        assert body["status"] == "ready", pathway["pathway_name"]
