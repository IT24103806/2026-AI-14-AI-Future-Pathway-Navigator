from fastapi.testclient import TestClient

from agent_1_conversation.agent import fallback_extract_slots, run_agent_1_turn
from agent_1_conversation.schemas import ExtractedProfileSlots
from main import app


def test_agent_1_extracts_complete_profile_without_api_key(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)

    response = run_agent_1_turn(
        user_message=(
            "I am an undergraduate at SLIIT. I know Python and problem solving, "
            "I enjoy robotics and AI, and I want to become an AI engineer."
        ),
        history=[],
        current_slots=ExtractedProfileSlots(),
    )

    assert response.is_complete is True
    assert response.extracted_slots.academic_stage == "Undergraduate"
    assert "Python" in response.extracted_slots.core_skills
    assert "Robotics" in response.extracted_slots.hobbies_interests
    assert response.extracted_slots.career_ambitions == "AI Engineer"
    assert response.missing_slots == []


def test_agent_1_preserves_existing_slots_and_requests_next_missing_field(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    existing = ExtractedProfileSlots(academic_stage="After A/L")

    response = run_agent_1_turn(
        user_message="I know Java and SQL.",
        history=[],
        current_slots=existing,
    )

    assert response.is_complete is False
    assert response.extracted_slots.academic_stage == "After A/L"
    assert {"Java", "SQL"}.issubset(set(response.extracted_slots.core_skills))
    assert "hobbies_interests" in response.missing_slots[0]


def test_agent_1_health_endpoint():
    client = TestClient(app)
    response = client.get("/api/v1/agent-1/health")

    assert response.status_code == 200
    assert response.json() == {"status": "healthy", "agent": "agent_1_conversation"}


def test_fallback_does_not_duplicate_case_insensitive_skills():
    slots = ExtractedProfileSlots(core_skills=["Python"])
    updated = fallback_extract_slots("I use python", slots)

    assert updated.core_skills == ["Python"]
