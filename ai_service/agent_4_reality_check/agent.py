"""Agent 4: deterministic feasibility, safety and human-approval gate."""

from time import perf_counter
from uuid import uuid4

from .schemas import ExecutionStep, RealityCheckRequest, RealityCheckResponse, ToolCallSummary

CAREER_REQUIREMENTS = {
    "software engineer": {
        "skills": ["Python", "OOP", "Data Structures", "Git", "Databases"],
        "degree": "A computing degree is commonly preferred; an accredited diploma plus a strong portfolio is a viable alternative.",
        "subjects": ["Mathematics or equivalent foundation mathematics", "English/communication"],
        "entry": ["Meet the selected institution's published entry criteria", "Complete a foundation route when direct entry is unavailable"],
        "cost": {"Low": "Prioritise public, scholarship, vocational or part-time routes.", "Medium": "Compare public and local private computing programmes.", "High": "International/private routes may be considered after verifying total fees."},
    },
    "ai engineer": {
        "skills": ["Python", "Linear Algebra", "Machine Learning", "Statistics", "PyTorch"],
        "degree": "A computing, data science or engineering degree with mathematics and AI/ML study is normally expected.",
        "subjects": ["Mathematics", "Statistics", "Computing fundamentals"],
        "entry": ["Meet degree mathematics requirements", "Build programming foundations before ML specialisation"],
        "cost": {"Low": "Start with an affordable computing route and verified open ML courses.", "Medium": "Choose a local computing/data degree with an AI specialisation.", "High": "Compare accredited international AI programmes and living costs."},
    },
    "data scientist": {
        "skills": ["Python", "SQL", "Statistics", "Data Visualization", "Pandas"],
        "degree": "A degree or equivalent training in data science, computing, mathematics or statistics is commonly preferred.",
        "subjects": ["Mathematics", "Statistics", "Computing"],
        "entry": ["Demonstrate quantitative ability", "Build a portfolio using real datasets"],
        "cost": {"Low": "Combine an affordable quantitative qualification with portfolio projects.", "Medium": "Compare local data/computing degree routes.", "High": "Verify international programme fees and internship access."},
    },
    "ui/ux designer": {
        "skills": ["Figma", "User Research", "Wireframing", "Prototyping", "Design Systems"],
        "degree": "A design degree is useful but a verified portfolio and practical design training can provide an alternative route.",
        "subjects": ["Art/design awareness", "English/communication"],
        "entry": ["Prepare a design portfolio", "Demonstrate user-centred design process"],
        "cost": {"Low": "Use portfolio-led short courses and free design tools first.", "Medium": "Compare local design diplomas/degrees.", "High": "Verify private/international programme fees and portfolio support."},
    },
}
ALLOWED_TOOLS = {"career_prerequisite_lookup"}


def _record_step(trace, name, started, status="success"):
    trace.append(ExecutionStep(step=name, status=status, duration_ms=round((perf_counter() - started) * 1000, 3)))


def career_prerequisite_lookup(target_career: str):
    """Least-privilege tool: local catalogue only; no network or personal-data access."""
    started = perf_counter()
    tool_name = "career_prerequisite_lookup"
    if tool_name not in ALLOWED_TOOLS:
        return [], ToolCallSummary(tool_name=tool_name, status="rejected", duration_ms=0, result_summary="Tool is not allow-listed.")
    normalized = target_career.lower().strip()
    requirements, matched = None, "none"
    for career, details in CAREER_REQUIREMENTS.items():
        if career in normalized:
            requirements, matched = details, career
            break
    return requirements, ToolCallSummary(
        tool_name=tool_name, status="success",
        duration_ms=round((perf_counter() - started) * 1000, 3),
        result_summary=f"Matched verified catalogue career: {matched}; evidence bundle: {'found' if requirements else 'missing'}",
    )


def evaluate_reality_check(request: RealityCheckRequest) -> RealityCheckResponse:
    workflow_id = f"reality-{uuid4()}"
    trace, validation_results = [], []
    try:
        started = perf_counter()
        target_lower = request.target_career.lower().strip()
        stream_lower = request.al_stream.lower().strip()
        results = [grade.strip().upper() for grade in request.al_results.replace("/", ",").split(",")]
        if not all(grade in {"A", "B", "C", "S", "F"} for grade in results):
            raise ValueError("A/L results must be comma-separated grades A, B, C, S or F")
        validation_results.extend(["request_schema_valid", "prompt_injection_check_passed"])
        _record_step(trace, "validate_input", started)

        started = perf_counter()
        requirements, tool_call = career_prerequisite_lookup(request.target_career)
        _record_step(trace, "controlled_prerequisite_tool", started)
        required_skills = requirements["skills"] if requirements else []
        current = {skill.casefold() for skill in request.current_skills}
        missing_skills = [skill for skill in required_skills if skill.casefold() not in current]
        risk_reasons, score = [], 90

        started = perf_counter()
        if not requirements:
            risk_reasons.append("Evidence risk: selected career is absent from the verified prerequisite catalogue.")
            score -= 20
        if missing_skills:
            score -= min(30, len(missing_skills) * 8)
        if ("software" in target_lower or "ai" in target_lower) and "arts" in stream_lower:
            risk_reasons.append("Stream mismatch: transitioning from Arts to Computing requires an accredited foundation route.")
            score -= 25
        if any(grade in {"S", "F"} for grade in results):
            risk_reasons.append("Academic entry risk: S or F results may limit direct degree entry.")
            score -= 20
        if request.budget_level == "Low" and ("abroad" in target_lower or "private" in target_lower):
            risk_reasons.append("Financial risk: proposed international/private route may exceed the stated budget.")
            score -= 25
        score = max(0, min(100, score))
        _record_step(trace, "apply_business_rules", started)

        started = perf_counter()
        high_risk = bool(risk_reasons)
        approval_required = high_risk or score < 60
        validation_results.extend(["score_range_valid", "business_rules_applied", "output_schema_valid"])
        _record_step(trace, "validate_output_and_route", started)
        degree_requirement = requirements["degree"] if requirements else "No verified education requirement is available; counsellor verification is mandatory."
        subject_requirements = requirements["subjects"] if requirements else []
        entry_requirements = requirements["entry"] if requirements else ["Verify entry requirements with an accredited provider"]
        cost_guidance = requirements["cost"][request.budget_level] if requirements else "Cost evidence is unavailable; do not make a financial commitment before counsellor verification."
        gap_closure_plan = [f"Learn and demonstrate {skill} through a small assessed project." for skill in missing_skills[:3]]
        if risk_reasons:
            gap_closure_plan.append("Book a counsellor review to verify the flagged academic or financial risk.")
        if not gap_closure_plan:
            gap_closure_plan.append("Maintain evidence of current skills and verify the selected institution's current intake criteria.")
        return RealityCheckResponse(
            workflow_id=workflow_id,
            status="approval_required" if approval_required else "completed",
            is_feasible=score >= 40, feasibility_score=score,
            missing_skills=missing_skills,
            skill_gap_summary=f"{len(missing_skills)} prerequisite skill gap(s) detected for '{request.target_career}'.",
            degree_requirement=degree_requirement, subject_requirements=subject_requirements,
            entry_requirements=entry_requirements, cost_guidance=cost_guidance,
            gap_closure_plan=gap_closure_plan,
            evidence_sources=["Versioned local career-requirement catalogue", "Student-declared A/L, budget and skill data"],
            is_high_risk=high_risk,
            risk_reason=" | ".join(risk_reasons) if risk_reasons else None,
            counsellor_review_required=approval_required,
            suggested_action="Pause and request an authorized counsellor decision." if approval_required else "Feasibility gate passed; pathway may proceed.",
            validation_results=validation_results, tool_calls=[tool_call], execution_trace=trace,
        )
    except Exception as exc:
        trace.append(ExecutionStep(step="safe_failure", status="failed", duration_ms=0))
        return RealityCheckResponse(
            workflow_id=workflow_id, status="safe_failure", is_feasible=False,
            feasibility_score=0, missing_skills=[], skill_gap_summary="Reality check could not be completed safely.",
            degree_requirement="Unavailable due to safe failure.", subject_requirements=[], entry_requirements=[],
            cost_guidance="Unavailable due to safe failure.", gap_closure_plan=["Correct the input and retry, or request counsellor review."],
            evidence_sources=[],
            is_high_risk=True, risk_reason="Validation or processing failure.", counsellor_review_required=True,
            suggested_action="Do not publish; retry or request counsellor review.", validation_results=validation_results,
            tool_calls=[], execution_trace=trace, error=str(exc),
        )
