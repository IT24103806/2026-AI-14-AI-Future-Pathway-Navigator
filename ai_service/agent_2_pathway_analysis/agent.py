import json
import time
import uuid
from typing import Callable, Dict, Any, List

from langchain_core.messages import HumanMessage, SystemMessage
from langgraph.graph import StateGraph, END

from core.config import settings
from .schemas import (
    StudentProfileInput,
    CareerCandidate,
    CareerPathRecommendation,
    Agent2AnalysisResponse,
)
from .state import Agent2State
from .knowledge_base import CAREER_PATHWAYS, ROADMAP_PHASE_LABELS
from .market_data import get_market_data
from .prompts import REASONING_SYSTEM_PROMPT

TOP_N_CANDIDATES = 3


def get_llm():
    """Same provider pattern as Agent 1 — falls back gracefully if no key is set."""
    if settings.LLM_PROVIDER == "google" and settings.GOOGLE_API_KEY:
        try:
            from langchain_google_genai import ChatGoogleGenerativeAI
            return ChatGoogleGenerativeAI(
                model=settings.GOOGLE_MODEL,
                google_api_key=settings.GOOGLE_API_KEY,
                temperature=0.3,
            )
        except Exception as e:
            print(f"Error initializing Google Gemini LLM: {e}")
    elif settings.LLM_PROVIDER == "openai" and settings.OPENAI_API_KEY:
        try:
            from langchain_openai import ChatOpenAI
            return ChatOpenAI(
                model=settings.OPENAI_MODEL,
                openai_api_key=settings.OPENAI_API_KEY,
                temperature=0.3,
            )
        except Exception as e:
            print(f"Error initializing OpenAI LLM: {e}")
    return None


# ----------------- Deterministic matching (the "domain analysis") -----------------

def _score_pathway(pathway: dict, profile: StudentProfileInput) -> tuple[int, List[str], List[str], List[str]]:
    """Weighted overlap score: skills 45%, interests 30%, career-ambition text 25%.
    Also returns missing_skills (required skills the student doesn't have) —
    computed deterministically from the same set operation, no LLM involved."""
    student_skills = {s.lower().strip() for s in profile.core_skills}
    student_interests = {s.lower().strip() for s in profile.hobbies_interests}
    ambition_text = (profile.career_ambitions or "").lower()

    req_skills = set(pathway["required_skills"])
    rel_interests = set(pathway["related_interests"])
    terms = pathway["matching_career_terms"]

    matched_skills = sorted(student_skills & req_skills)
    matched_interests = sorted(student_interests & rel_interests)
    missing_skills = sorted(req_skills - student_skills)

    skill_score = (len(matched_skills) / len(req_skills)) if req_skills else 0
    interest_score = (len(matched_interests) / len(rel_interests)) if rel_interests else 0
    ambition_score = 1.0 if any(t in ambition_text for t in terms) else 0.0

    total = (skill_score * 0.45) + (interest_score * 0.30) + (ambition_score * 0.25)
    return round(total * 100), matched_skills, matched_interests, missing_skills


def _get_pathway_def(pathway_name: str) -> dict:
    return next((p for p in CAREER_PATHWAYS if p["pathway_name"] == pathway_name), {})


def _build_roadmap(pathway_name: str) -> List[dict]:
    """Deterministic roadmap: zip each pathway's ordered recommended_courses
    with the fixed phase labels. No LLM involved -> no invented course order."""
    courses = _get_pathway_def(pathway_name).get("recommended_courses", [])
    return [
        {"phase": phase, "course": course}
        for phase, course in zip(ROADMAP_PHASE_LABELS, courses)
    ]


def plan_node(state: Agent2State) -> Dict[str, Any]:
    """Node 1: explicit multi-step plan (persisted for auditability)."""
    plan = [
        "match_profile_against_pathway_knowledge_base",
        "select_top_candidate_pathways",
        "fetch_real_market_demand_and_competition_data",
        "generate_explained_reasoning_per_candidate",
        "validate_outputs_against_business_rules",
    ]
    return {"plan": plan}


def match_candidates_node(state: Agent2State) -> Dict[str, Any]:
    """Node 2: deterministic domain-analysis matching against the allow-listed KB."""
    profile = state["profile"]
    scored = []
    for pathway in CAREER_PATHWAYS:
        score, matched_skills, matched_interests, missing_skills = _score_pathway(pathway, profile)
        scored.append((score, pathway, matched_skills, matched_interests, missing_skills))

    scored.sort(key=lambda x: x[0], reverse=True)
    top = scored[:TOP_N_CANDIDATES]

    candidates = [
        CareerCandidate(
            pathway_name=pathway["pathway_name"],
            match_score=score,
            matched_skills=matched_skills,
            matched_interests=matched_interests,
            missing_skills=missing_skills,
            search_term=pathway["search_term"],
        )
        for score, pathway, matched_skills, matched_interests, missing_skills in top
    ]
    return {"candidates": candidates}


def market_data_node(state: Agent2State) -> Dict[str, Any]:
    """Node 3: controlled tool call — one validated call per candidate, least privilege."""
    candidates = state["candidates"]
    enriched = []
    for c in candidates:
        market = get_market_data(c.search_term)  # allow-listed tool, validated input
        enriched.append(c.model_copy(update={"market_data": market}))
    return {"candidates": enriched}


def _build_recommendation_from_kb(candidate: CareerCandidate, label: str, reasoning_text: str) -> CareerPathRecommendation:
    md = candidate.market_data
    kb_entry = _get_pathway_def(candidate.pathway_name)
    return CareerPathRecommendation(
        label=label,
        pathway_name=candidate.pathway_name,
        match_score=candidate.match_score,
        demand_score=md.demand_score,
        competition_score=md.competition_score,
        trend=md.trend,
        data_source=md.data_source,
        reasoning=reasoning_text,
        missing_skills=candidate.missing_skills,
        recommended_courses=kb_entry.get("recommended_courses", []),
        roadmap=_build_roadmap(candidate.pathway_name),
        day_in_the_life=kb_entry.get("day_in_the_life", ""),
        salary_range_lkr=kb_entry.get("salary_range_lkr", ""),
        industry_tools=kb_entry.get("industry_tools", []),
        portfolio_projects=kb_entry.get("portfolio_projects", []),
        recommended_certifications=kb_entry.get("recommended_certifications", []),
        sri_lankan_education_routes=kb_entry.get("sri_lankan_education_routes", []),
    )


def _fallback_reasoning(candidate: CareerCandidate, label: str) -> CareerPathRecommendation:
    md = candidate.market_data
    trend_phrase = {
        "rising": "postings have been trending up recently",
        "declining": "postings have slowed down recently",
        "stable": "postings have stayed fairly steady",
    }[md.trend]
    reasoning = (
        f"This matches your background through {', '.join(candidate.matched_skills) or 'your stated interests'} "
        f"and {', '.join(candidate.matched_interests) or 'related interests'}. "
        f"Current market data shows a demand score of {md.demand_score}/100 with "
        f"competition around {md.competition_score}/100, and {trend_phrase}."
    )
    return _build_recommendation_from_kb(candidate, label, reasoning)


def reasoning_node(state: Agent2State) -> Dict[str, Any]:
    """Node 4: LLM explains the fixed candidate set; never allowed to invent new pathways."""
    candidates = state["candidates"]
    profile = state["profile"]
    labels = [f"Path {chr(65 + i)}" for i in range(len(candidates))]  # Path A, B, C...
    llm = get_llm()

    if llm is None:
        recs = [_fallback_reasoning(c, labels[i]) for i, c in enumerate(candidates)]
        return {"recommendations": recs}

    try:
        candidates_json = json.dumps([c.model_dump() for c in candidates], default=str)
        prompt_text = REASONING_SYSTEM_PROMPT.format(
            candidates_json=candidates_json,
            academic_stage=profile.academic_stage or "Not specified",
            core_skills=", ".join(profile.core_skills) or "Not specified",
            hobbies_interests=", ".join(profile.hobbies_interests) or "Not specified",
            career_ambitions=profile.career_ambitions or "Not specified",
        )
        resp = llm.invoke([SystemMessage(content=prompt_text), HumanMessage(content="Generate the reasoning JSON now.")])

        # Expect the LLM to return a JSON list of {pathway_name, reasoning}
        raw = resp.content.strip().strip("```json").strip("```")
        parsed = json.loads(raw)
        reasoning_by_name = {item["pathway_name"]: item["reasoning"] for item in parsed}

        recs = []
        for i, c in enumerate(candidates):
            reasoning_text = reasoning_by_name.get(c.pathway_name)
            if not reasoning_text:
                recs.append(_fallback_reasoning(c, labels[i]))
                continue
            recs.append(_build_recommendation_from_kb(c, labels[i], reasoning_text))
        return {"recommendations": recs}
    except Exception as e:
        print(f"[Agent2] LLM reasoning failed, using fallback: {e}")
        recs = [_fallback_reasoning(c, labels[i]) for i, c in enumerate(candidates)]
        return {"recommendations": recs}


def validation_node(state: Agent2State) -> Dict[str, Any]:
    """Node 5: deterministic guardrails — this is what stops hallucinated output from reaching a human."""
    candidates = state["candidates"]
    recommendations = state["recommendations"]
    errors: List[str] = []

    allowed_names = {c.pathway_name for c in candidates}
    seen_names = set()

    if not recommendations:
        errors.append("No recommendations were generated.")

    candidate_by_name = {c.pathway_name: c for c in candidates}

    for rec in recommendations:
        if rec.pathway_name not in allowed_names:
            errors.append(f"Recommendation '{rec.pathway_name}' is not one of the matched candidates (possible hallucination).")
        if rec.pathway_name in seen_names:
            errors.append(f"Duplicate recommendation for '{rec.pathway_name}'.")
        seen_names.add(rec.pathway_name)
        if not (0 <= rec.match_score <= 100) or not (0 <= rec.demand_score <= 100) or not (0 <= rec.competition_score <= 100):
            errors.append(f"Score out of range for '{rec.pathway_name}'.")
        if rec.trend not in ("rising", "stable", "declining"):
            errors.append(f"Invalid trend value for '{rec.pathway_name}'.")
        if not rec.recommended_courses:
            errors.append(f"No recommended courses for '{rec.pathway_name}'.")
        if not rec.roadmap or len(rec.roadmap) != len(rec.recommended_courses):
            errors.append(f"Roadmap doesn't match recommended courses for '{rec.pathway_name}'.")
        if not rec.reasoning or len(rec.reasoning.strip()) < 10:
            errors.append(f"Reasoning missing or too short for '{rec.pathway_name}'.")

        # Missing-skills must match what deterministic matching computed —
        # the LLM is not allowed to alter this list (guards against the LLM
        # inventing or hiding skill gaps in the explanation step).
        source_candidate = candidate_by_name.get(rec.pathway_name)
        if source_candidate and set(rec.missing_skills) != set(source_candidate.missing_skills):
            errors.append(f"missing_skills for '{rec.pathway_name}' was altered from the matching step.")

        # Enriched KB fields (projects, certifications, Sri Lankan routes) must match the curated KB if present.
        kb_entry = _get_pathway_def(rec.pathway_name)
        if kb_entry:
            if rec.portfolio_projects and rec.portfolio_projects != kb_entry.get("portfolio_projects", []):
                errors.append(f"portfolio_projects for '{rec.pathway_name}' was altered from the knowledge base.")
            if rec.recommended_certifications and rec.recommended_certifications != kb_entry.get("recommended_certifications", []):
                errors.append(f"recommended_certifications for '{rec.pathway_name}' was altered from the knowledge base.")
            if rec.sri_lankan_education_routes and rec.sri_lankan_education_routes != kb_entry.get("sri_lankan_education_routes", []):
                errors.append(f"sri_lankan_education_routes for '{rec.pathway_name}' was altered from the knowledge base.")

    return {"validation_errors": errors, "is_valid": len(errors) == 0}


def finalize_node(state: Agent2State) -> Dict[str, Any]:
    """Node 6: sets the persisted status. 'pending_approval' is the pause for human sign-off —
    an advisor/student must approve before this is written into the student's official profile."""
    if state["is_valid"]:
        return {"status": "pending_approval"}
    return {"status": "failed"}


def _traced(node_name: str, fn: Callable[[Agent2State], Dict[str, Any]]) -> Callable[[Agent2State], Dict[str, Any]]:
    """Wraps a node so every run records {node, duration_ms, status} into
    execution_trace — observability evidence for the viva, no behavior change."""

    def wrapper(state: Agent2State) -> Dict[str, Any]:
        start = time.perf_counter()
        try:
            result = fn(state) or {}
        except Exception:
            # A raised exception aborts the graph run entirely — no partial
            # state update is applied — so run_agent_2_analysis's outer
            # try/except is what turns this into a structured "failed" response.
            raise
        duration_ms = round((time.perf_counter() - start) * 1000, 2)
        result["execution_trace"] = state.get("execution_trace", []) + [
            {"node": node_name, "duration_ms": duration_ms, "status": "success"}
        ]
        return result

    return wrapper


def create_agent_2_graph():
    workflow = StateGraph(Agent2State)
    workflow.add_node("plan", _traced("plan", plan_node))
    workflow.add_node("match_candidates", _traced("match_candidates", match_candidates_node))
    workflow.add_node("fetch_market_data", _traced("fetch_market_data", market_data_node))
    workflow.add_node("generate_reasoning", _traced("generate_reasoning", reasoning_node))
    workflow.add_node("validate", _traced("validate", validation_node))
    workflow.add_node("finalize", _traced("finalize", finalize_node))

    workflow.set_entry_point("plan")
    workflow.add_edge("plan", "match_candidates")
    workflow.add_edge("match_candidates", "fetch_market_data")
    workflow.add_edge("fetch_market_data", "generate_reasoning")
    workflow.add_edge("generate_reasoning", "validate")
    workflow.add_edge("validate", "finalize")
    workflow.add_edge("finalize", END)

    return workflow.compile()


agent_2_app = create_agent_2_graph()


def run_agent_2_analysis(profile: StudentProfileInput) -> Agent2AnalysisResponse:
    """Executes the full Agent 2 workflow for one student profile.

    Safe-failure wrapper: an unexpected exception anywhere in the graph
    (LLM outage, malformed data, etc.) must never surface as a raw 500 —
    it is caught here and turned into a structured 'failed' response so
    the caller always gets a well-formed Agent2AnalysisResponse.
    """
    workflow_id = str(uuid.uuid4())
    initial_state: Agent2State = {
        "workflow_id": workflow_id,
        "profile": profile,
        "plan": [],
        "candidates": [],
        "recommendations": [],
        "validation_errors": [],
        "is_valid": False,
        "status": None,
        "execution_trace": [],
    }

    try:
        final_state = agent_2_app.invoke(initial_state)
    except Exception as e:
        print(f"[Agent2] Workflow failed unexpectedly: {e}")
        return Agent2AnalysisResponse(
            workflow_id=workflow_id,
            status="failed",
            recommendations=[],
            validation_errors=[f"Agent 2 workflow failed unexpectedly: {e}"],
            execution_trace=[],
        )

    return Agent2AnalysisResponse(
        workflow_id=workflow_id,
        status=final_state["status"],
        recommendations=final_state["recommendations"],
        validation_errors=final_state["validation_errors"],
        execution_trace=final_state.get("execution_trace", []),
    )
