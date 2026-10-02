from fastapi.testclient import TestClient

from agent_1_conversation.agent import fallback_extract_slots, run_agent_1_turn, slots_changed
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


# ---------------------------------------------------------------- profile updates

def _saved_profile() -> ExtractedProfileSlots:
    return ExtractedProfileSlots(
        full_name="Nimal Perera",
        academic_stage="Undergraduate",
        core_skills=["Python", "SQL"],
        hobbies_interests=["AI", "Gaming"],
        career_ambitions="AI Engineer",
    )


def test_update_session_does_not_finish_before_anything_changed(monkeypatch):
    """'Re-run AI onboarding' must not complete (and re-save) while the student has changed nothing."""
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    saved = _saved_profile()

    response = run_agent_1_turn(
        user_message="Hi, I would like to review my profile.",
        history=[],
        current_slots=saved,
        update_mode=True,
        baseline_slots=saved,
    )

    assert response.is_complete is False
    assert response.extracted_slots == saved
    assert "update" in response.reply_message.lower()


def test_update_session_completes_when_a_skill_is_added(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    saved = _saved_profile()

    response = run_agent_1_turn(
        user_message="Please add Docker to my skills.",
        history=[],
        current_slots=saved,
        update_mode=True,
        baseline_slots=saved,
    )

    assert response.is_complete is True
    assert "Docker" in response.extracted_slots.core_skills
    # Nothing else from the saved profile is lost.
    assert {"Python", "SQL"}.issubset(set(response.extracted_slots.core_skills))
    assert response.extracted_slots.career_ambitions == "AI Engineer"


def test_update_session_accepts_a_name_correction(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    saved = _saved_profile()

    response = run_agent_1_turn(
        user_message="Actually my name is Nimal Jayasuriya.",
        history=[],
        current_slots=saved,
        update_mode=True,
        baseline_slots=saved,
    )

    assert response.is_complete is True
    assert response.extracted_slots.full_name == "Nimal Jayasuriya"


def test_update_session_leaves_the_caller_profile_untouched(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    saved = _saved_profile()

    run_agent_1_turn(
        user_message="I learned Rust and Figma",
        history=[],
        current_slots=saved,
        update_mode=True,
        baseline_slots=saved,
    )

    assert saved.core_skills == ["Python", "SQL"]


def test_self_introduction_is_not_mistaken_for_a_name():
    slots = fallback_extract_slots("I am an undergraduate at SLIIT", ExtractedProfileSlots())
    assert slots.full_name is None


def test_explicit_name_statement_is_extracted():
    slots = fallback_extract_slots("Hi, my name is Nimal Perera", ExtractedProfileSlots())
    assert slots.full_name == "Nimal Perera"


def test_slots_changed_compares_names_and_lists_case_insensitively():
    saved = _saved_profile()

    assert slots_changed(saved, saved) is False
    assert slots_changed(saved, saved.model_copy(update={"full_name": "Nimal J"})) is True
    assert slots_changed(saved, saved.model_copy(update={"core_skills": ["python", "sql"]})) is False
    assert slots_changed(saved, saved.model_copy(update={"hobbies_interests": ["AI", "Gaming", "Chess"]})) is True


def test_graduated_is_not_confused_with_undergraduate():
    """'undergraduate' contains the letters of 'graduat', so the stage check must be word-boundaried."""
    assert fallback_extract_slots(
        "I am an undergraduate at SLIIT", ExtractedProfileSlots()
    ).academic_stage == "Undergraduate"

    assert fallback_extract_slots(
        "I finished my degree and graduated last year", ExtractedProfileSlots()
    ).academic_stage == "Graduated"


def test_update_session_recognises_a_stage_change(monkeypatch):
    monkeypatch.setattr("agent_1_conversation.agent.get_llm", lambda: None)
    saved = _saved_profile()

    response = run_agent_1_turn(
        user_message="I graduated last month, so I am a graduate now.",
        history=[],
        current_slots=saved,
        update_mode=True,
        baseline_slots=saved,
    )

    assert response.is_complete is True
    assert response.extracted_slots.academic_stage == "Graduated"
