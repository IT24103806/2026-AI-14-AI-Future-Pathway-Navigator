from .schemas import RealityCheckRequest, RealityCheckResponse

CAREER_PREREQUISITES = {
    "software engineer": ["Python", "OOP", "Data Structures", "Git", "Databases"],
    "ai engineer": ["Python", "Linear Algebra", "Machine Learning", "Statistics", "PyTorch"],
    "data scientist": ["Python", "SQL", "Statistics", "Data Visualization", "Pandas"],
    "ui/ux designer": ["Figma", "User Research", "Wireframing", "Prototyping", "Design Systems"],
}

def evaluate_reality_check(request: RealityCheckRequest) -> RealityCheckResponse:
    target_lower = request.target_career.lower().strip()
    stream_lower = request.al_stream.lower().strip()
    budget_lower = request.budget_level.lower().strip()
    results_lower = request.al_results.lower().strip()

    missing_skills = []
    risk_reasons = []
    score = 90
    is_high_risk = False

    # 1. Skill Gap Analysis
    matched_skills = []
    for career_key, req_skills in CAREER_PREREQUISITES.items():
        if career_key in target_lower:
            for skill in req_skills:
                if any(skill.lower() in s.lower() for s in request.current_skills):
                    matched_skills.append(skill)
                else:
                    missing_skills.append(skill)
            break

    if missing_skills:
        score -= min(30, len(missing_skills) * 8)

    # 2. Academic Stream & Results Feasibility Rule
    if ("software" in target_lower or "ai" in target_lower) and "arts" in stream_lower:
        is_high_risk = True
        risk_reasons.append("Stream mismatch: Transitioning from Arts to Computing requires an accredited foundation year.")
        score -= 25

    if any(grade in results_lower for grade in ["s", "f"]):
        is_high_risk = True
        risk_reasons.append("Academic entry risk: Contains S or F passes which limit direct degree entry.")
        score -= 20

    # 3. Budget Feasibility Rule
    if budget_lower == "low" and ("abroad" in target_lower or "private" in target_lower):
        is_high_risk = True
        risk_reasons.append("Financial risk: Proposed international/private institute fees exceed student budget limit.")
        score -= 25

    score = max(15, min(100, score))
    counsellor_review_required = is_high_risk or score < 60

    action = (
        "High risk flagged: Route to human Counsellor for review and approval."
        if counsellor_review_required
        else "Feasible: Proceed directly with the generated pathway roadmap."
    )

    return RealityCheckResponse(
        is_feasible=(score >= 40),
        feasibility_score=score,
        missing_skills=missing_skills if missing_skills else ["No major prerequisite skill gap identified"],
        skill_gap_summary=f"{len(missing_skills)} missing prerequisite skill(s) detected for target role '{request.target_career}'.",
        is_high_risk=is_high_risk,
        risk_reason=" | ".join(risk_reasons) if risk_reasons else None,
        counsellor_review_required=counsellor_review_required,
        suggested_action=action
    )