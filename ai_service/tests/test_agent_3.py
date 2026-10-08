from agent_3_pathway_planner.agent import run_pathway_planner
from agent_3_pathway_planner.schemas import PlannerProfile, PlannerRequest


def test_run_pathway_planner_handles_short_course_lists_and_whitespace(monkeypatch):
    custom_pathway = {
        "pathway_name": "Custom Pathway",
        "description": "A compact pathway used for planner validation.",
        "required_skills": [" Python ", "sql", " problem solving "],
        "related_interests": ["ai"],
        "matching_career_terms": ["custom pathway"],
        "recommended_courses": ["Intro to Python"],
        "search_term": "custom pathway",
    }

    monkeypatch.setattr("agent_3_pathway_planner.agent.CAREER_PATHWAYS", [custom_pathway])

    request = PlannerRequest(
        selected_pathway="custom pathway",
        profile=PlannerProfile(
            academic_stage="Undergraduate",
            core_skills=[" python ", " sql "],
            career_ambitions="I want to build intelligent systems",
        ),
        completed_phases=["education"],
    )

    response = run_pathway_planner(request)

    assert response.status == "ready"
    assert response.selected_pathway == "custom pathway"
    assert response.roadmap
    assert len(response.roadmap) == 6
    assert response.missing_skills == ["problem solving"]
    assert response.next_action
    assert all(stage.actions for stage in response.roadmap)
