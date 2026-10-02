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
    sl_routes = pathway.get("sri_lankan_education_routes", [])
    projects = pathway.get("portfolio_projects", [])
    certs = pathway.get("recommended_certifications", [])
    tools = pathway.get("industry_tools", [])
    completed = {phase.strip().casefold() for phase in profile.completed_phases if phase and phase.strip()}

    education_actions = [
        f"Compare accredited degree, diploma, and foundation options for {pathway['pathway_name']}.",
    ]
    if sl_routes:
        education_actions.append(f"Sri Lankan routes: {sl_routes[0]} | {sl_routes[-1]}.")
    education_actions.append("Confirm entry requirements, duration, and total cost before enrolling.")

    skill_actions = [f"Study {courses[0]}."]
    if missing_skills:
        skill_actions.append(f"Focus on closing your missing prerequisite skills: {', '.join(missing_skills)}.")
    if projects:
        skill_actions.append(f"Build starter portfolio project: {projects[0]}.")
    else:
        skill_actions.append(f"Practice through two portfolio projects related to {pathway['description'].lower()}")

    cert_actions = [f"Complete {courses[1]}."]
    if certs:
        cert_actions.append(f"Prepare for industry certification: {certs[0]}.")
    if len(projects) > 1:
        cert_actions.append(f"Publish capstone project evidence: {projects[1]}.")
    else:
        cert_actions.append("Publish project evidence and document the tools used.")

    internship_actions = [
        "Apply to internships that mention the pathway title and your completed skills.",
    ]
    if tools:
        internship_actions.append(f"Demonstrate hands-on proficiency with industry tools: {', '.join(tools[:5])}.")
    internship_actions.append("Ask for feedback and turn one internship project into a portfolio case study.")

    stages = [
        ("education", "Build the education foundation", education_actions, "3-4 years"),
        ("skills", "Close the core skill gaps", skill_actions, "3-6 months"),
        ("certification", "Add evidence of readiness", cert_actions, "3-6 months"),
        ("internship", "Get supervised industry experience", internship_actions, "3-6 months"),
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
    completed_phases = [step.stage for step in roadmap if step.status == "completed"]
    next_stage = next((step for step in roadmap if step.status != "completed"), None)
    next_action = (
        next_stage.actions[0]
        if next_stage is not None
        else f"All roadmap milestones for {pathway['pathway_name']} are completed! Keep your portfolio and skills updated."
    )
    return {
        "roadmap": roadmap,
        "missing_skills": missing_skills,
        "completed_phases": completed_phases,
        "next_action": next_action,
    }


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
        "completed_phases": [],
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
        completed_phases=state.get("completed_phases", []),
        next_action=state.get("next_action", ""),
        validation_errors=state["validation_errors"],
        execution_trace=state["execution_trace"],
    )