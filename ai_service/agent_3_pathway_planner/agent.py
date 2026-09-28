import time
import uuid
from typing import Any, Dict

from langgraph.graph import END, StateGraph

from agent_2_pathway_analysis.knowledge_base import CAREER_PATHWAYS
from .schemas import PlannerRequest, PlannerResponse, RoadmapStage
from .state import PlannerState


def _find_pathway(name: str) -> dict | None:
    normalized = name.strip().casefold()
    return next((pathway for pathway in CAREER_PATHWAYS if pathway["pathway_name"].casefold() == normalized), None)


def _build_stages(pathway: dict, profile: PlannerRequest, missing_skills: list[str]) -> list[RoadmapStage]:
    courses = pathway["recommended_courses"]
    completed = {phase.casefold() for phase in profile.completed_phases}
    stages = [
        ("education", "Build the education foundation", [
            f"Compare accredited degree, diploma, and foundation options for {pathway['pathway_name']}.",
            "Confirm entry requirements, duration, and total cost before enrolling.",
        ], "3-4 years"),
        ("skills", "Close the core skill gaps", [
            f"Study {courses[0]}.",
            f"Practice through two portfolio projects related to {pathway['description'].lower()}",
        ], "3-6 months"),
        ("certification", "Add evidence of readiness", [
            f"Complete {courses[1]}.",
            "Publish project evidence and document the tools used.",
        ], "3-6 months"),
        ("internship", "Get supervised industry experience", [
            "Apply to internships that mention the pathway title and your completed skills.",
            "Ask for feedback and turn one internship project into a portfolio case study.",
        ], "3-6 months"),
        ("first_job", "Target the first relevant role", [
            f"Apply for junior {pathway['search_term']} roles.",
            "Tailor your CV to demonstrated skills instead of listing only course names.",
        ], "1-3 months"),
        ("long_term_growth", "Plan the next career level", [
            f"Progress to {courses[2]} or an equivalent advanced qualification.",
            "Review market demand and update the roadmap every six months.",
        ], "1-2 years"),
    ]
    return [
        RoadmapStage(
            order=index,
            stage=stage,
            title=title,
            outcome=f"Reach the {stage.replace('_', ' ')} milestone for {pathway['pathway_name']}.",
            actions=actions,
            estimated_duration=duration,
            status="completed" if stage.casefold() in completed else "not_started",
        )
        for index, (stage, title, actions, duration) in enumerate(stages, start=1)
    ]


def validate_node(state: Dict[str, Any]) -> Dict[str, Any]:
    pathway = _find_pathway(state["request"].selected_pathway)
    return {"pathway": pathway, "validation_errors": [] if pathway else [
        f"Selected pathway '{state['request'].selected_pathway}' is not available in the curated knowledge base."
    ]}


def build_roadmap_node(state: Dict[str, Any]) -> Dict[str, Any]:
    pathway = state["pathway"]
    request = state["request"]
    student_skills = {skill.strip().casefold() for skill in request.profile.core_skills}
    missing_skills = sorted(skill for skill in pathway["required_skills"] if skill.casefold() not in student_skills)
    roadmap = _build_stages(pathway, request, missing_skills)
    next_stage = next((step for step in roadmap if step.status != "completed"), roadmap[-1])
    return {"roadmap": roadmap, "missing_skills": missing_skills, "next_action": next_stage.actions[0]}


def finalize_node(state: Dict[str, Any]) -> Dict[str, Any]:
    return {"status": "ready" if not state["validation_errors"] else "failed"}


def _traced(name: str, function):
    def wrapper(state):
        started = time.perf_counter()
        result = function(state)
        trace = state.get("execution_trace", []) + [{
            "node": name,
            "duration_ms": round((time.perf_counter() - started) * 1000, 2),
            "status": "success",
        }]
        result["execution_trace"] = trace
        return result
    return wrapper


def create_planner_graph():
    workflow = StateGraph(PlannerState)
    workflow.add_node("validate_selected_pathway", _traced("validate_selected_pathway", validate_node))
    workflow.add_node("build_roadmap", _traced("build_roadmap", build_roadmap_node))
    workflow.add_node("finalize", _traced("finalize", finalize_node))
    workflow.set_entry_point("validate_selected_pathway")
    workflow.add_conditional_edges(
        "validate_selected_pathway",
        lambda state: "build_roadmap" if state["pathway"] else "finalize",
        {"build_roadmap": "build_roadmap", "finalize": "finalize"},
    )
    workflow.add_edge("build_roadmap", "finalize")
    workflow.add_edge("finalize", END)
    return workflow.compile()


planner_app = create_planner_graph()


def run_pathway_planner(request: PlannerRequest) -> PlannerResponse:
    workflow_id = str(uuid.uuid4())
    state = planner_app.invoke({
        "request": request,
        "pathway": None,
        "roadmap": [],
        "missing_skills": [],
        "next_action": "",
        "validation_errors": [],
        "status": "failed",
        "execution_trace": [],
    })
    return PlannerResponse(
        workflow_id=workflow_id,
        status=state["status"],
        selected_pathway=request.selected_pathway,
        roadmap=state.get("roadmap", []),
        missing_skills=state.get("missing_skills", []),
        next_action=state.get("next_action", ""),
        validation_errors=state["validation_errors"],
        execution_trace=state["execution_trace"],
    )