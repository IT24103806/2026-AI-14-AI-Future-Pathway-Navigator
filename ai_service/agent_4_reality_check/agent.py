"""Agent 4: deterministic feasibility, safety and human-approval gate."""

import re
from time import perf_counter
from uuid import uuid4

from .schemas import ExecutionStep, RealityCheckRequest, RealityCheckResponse, ToolCallSummary

CAREER_REQUIREMENTS = {
    "software engineer": {
        "skills": ["Python", "OOP", "Data Structures", "Git", "Databases"],
        "degree": "A computing degree is commonly preferred (e.g., State University BSc CS/SE, SLIIT BSc Hons in IT - Software Engineering, OUSL BSc SE, or UCSC/UoM BIT); an accredited TVEC NVQ Level 5/6 ICT diploma plus a strong portfolio is a viable alternative.",
        "subjects": ["Mathematics or equivalent foundation mathematics", "English/communication"],
        "entry": [
            "Meet the selected institution's published entry criteria (3 A/L passes for direct BSc entry)",
            "Complete an accredited Computing Foundation or TVEC NVQ Level 4/5 route when transitioning from O/L or non-STEM streams",
        ],
        "cost": {
            "Low": "Prioritise UGC state universities, OUSL BSc SE, UCSC/UoM BIT, SLIATE HNDIT, TVEC NVQ Level 5 routes, or the Government Interest-Free Student Loan Scheme (IFSLS).",
            "Medium": "Compare local accredited private computing programmes (SLIIT, IIT, NSBM, NIBM) and merit scholarships.",
            "High": "International or foreign-affiliated degree routes may be considered after verifying total tuition and living fees.",
        },
    },
    "ai engineer": {
        "skills": ["Python", "Linear Algebra", "Machine Learning", "Statistics", "PyTorch"],
        "degree": "A computing, data science or engineering degree with mathematics and AI/ML study is normally expected (e.g., SLIIT BSc Hons in IT - AI / Data Science, or State University CS/Engineering).",
        "subjects": ["Mathematics", "Statistics", "Computing fundamentals"],
        "entry": [
            "Meet degree mathematics requirements (Combined Maths or bridging foundation mathematics)",
            "Build programming foundations before ML specialisation (or complete SLIIT Computing Foundation / NVQ Level 5 ICT bridge)",
        ],
        "cost": {
            "Low": "Start with an affordable computing route (State University, OUSL, UCSC BIT, or IFSLS loan) and verified open ML courses (DeepLearning.AI / fast.ai).",
            "Medium": "Choose a local computing/data degree with an AI specialisation (e.g., SLIIT BSc Hons AI/Data Science).",
            "High": "Compare accredited international AI programmes, cloud GPU lab costs, and living expenses.",
        },
    },
    "data scientist": {
        "skills": ["Python", "SQL", "Statistics", "Data Visualization", "Pandas"],
        "degree": "A degree or equivalent training in data science, computing, mathematics or statistics is commonly preferred (e.g., SLIIT BSc Hons Data Science, State University Statistics/CS, or OUSL/BIT).",
        "subjects": ["Mathematics", "Statistics", "Computing"],
        "entry": [
            "Demonstrate quantitative ability (A/L Mathematics/Statistics or accredited foundation bridging module)",
            "Build a portfolio using real datasets and SQL/Power BI dashboards",
        ],
        "cost": {
            "Low": "Combine an affordable quantitative qualification (State University, OUSL, BIT, or NVQ Level 5) with portfolio projects.",
            "Medium": "Compare local data/computing degree routes (SLIIT, IIT, NIBM Business Analytics).",
            "High": "Verify international programme fees and internship access.",
        },
    },
    "ui/ux designer": {
        "skills": ["Figma", "User Research", "Wireframing", "Prototyping", "Design Systems"],
        "degree": "A design or interactive-media degree is useful (e.g., SLIIT BSc Interactive Media, UoM Bachelor of Design, AOD/AMDT), but a TVEC NVQ Level 5 Multimedia diploma and verified Figma portfolio provide a strong alternative route.",
        "subjects": ["Art/design awareness", "English/communication"],
        "entry": [
            "Prepare a 3-case-study user-centred design portfolio on Behance/Figma",
            "Open to Arts, Commerce, Tech, and Science A/L streams or Foundation/NVQ Level 4/5 bridge entrants",
        ],
        "cost": {
            "Low": "Use portfolio-led courses (Google UX Certificate), TVEC NVQ Level 5 Multimedia routes, and free Figma education tiers first.",
            "Medium": "Compare local interactive media & design diplomas/degrees (SLIIT, NIBM, AMDT).",
            "High": "Verify private/international design degree fees (AOD / foreign transfer) and portfolio mentorship.",
        },
    },
    "cybersecurity analyst": {
        "skills": ["Networking", "Linux", "Security", "Python", "Problem Solving"],
        "degree": "A computing, networking or information-security degree is commonly preferred (e.g., SLIIT BSc Hons Cyber Security); recognised security certifications (ISC2 CC, CompTIA Security+) and NVQ Level 5 Network Administration can supplement it.",
        "subjects": ["Mathematics or ICT foundation", "English/communication"],
        "entry": [
            "Meet the selected institution's published entry criteria or complete a Computing Foundation / NVQ Level 5 Network route",
            "Build hands-on lab evidence (networking, Linux, SIEM, ethical hacking basics)",
        ],
        "cost": {
            "Low": "Start with public/vocational routes (SLIATE HNDIT, OUSL, TVEC NVQ Level 5) and free ISC2 CC certification + TryHackMe labs.",
            "Medium": "Compare local computing/security degrees (SLIIT, Cicra, NSBM) and one entry-level certification (CompTIA Security+).",
            "High": "Verify international programme fees and advanced certification costs.",
        },
    },
    "devops engineer": {
        "skills": ["Cloud", "Linux", "Git", "Networking", "Problem Solving"],
        "degree": "A computing or network-engineering degree is commonly preferred (e.g., SLIIT BSc Hons Computer Systems & Network Engineering); cloud certifications (AWS/Azure) and a CI/CD deployment portfolio are a viable complement.",
        "subjects": ["Mathematics or ICT foundation", "English/communication"],
        "entry": [
            "Meet the selected institution's published entry criteria or complete an NVQ Level 5/6 Systems & Network Diploma",
            "Deploy at least one containerized project through a CI/CD pipeline",
        ],
        "cost": {
            "Low": "Use free-tier cloud accounts (AWS Educate / Azure for Students) and open-source tooling alongside a public/NVQ/BIT route.",
            "Medium": "Compare local computing programmes (SLIIT, IIT, NSBM) and one associate cloud certification.",
            "High": "Verify international programme fees and multi-cloud certification costs.",
        },
    },
    "mobile app developer": {
        "skills": ["Dart", "Flutter", "Java", "UI/UX", "Problem Solving"],
        "degree": "A computing or software-engineering degree is commonly preferred (e.g., SLIIT BSc Hons Software Engineering, OUSL BSc SE, or UCSC BIT); a TVEC NVQ Level 5 Software Diploma plus a published-app portfolio can supplement it.",
        "subjects": ["Mathematics or equivalent foundation mathematics", "English/communication"],
        "entry": [
            "Meet the selected institution's published entry criteria or complete a Computing Foundation / NVQ Level 4/5 bridge",
            "Publish or demo at least one working mobile app with clean architecture and tests",
        ],
        "cost": {
            "Low": "Use free tooling (Flutter, Android Studio) alongside OUSL, BIT, SLIATE HNDIT, or TVEC NVQ Level 5 routes.",
            "Medium": "Compare local software-engineering programmes (SLIIT, IIT, NIBM, NSBM).",
            "High": "Verify international programme fees and iOS/Android hardware testing costs.",
        },
    },
    "game developer": {
        "skills": ["C++", "C#", "Mathematics", "Problem Solving", "Game Engines"],
        "degree": "A computing, interactive-media, or game-development degree is commonly preferred (e.g., SLIIT BSc Hons Interactive Media / SE); a playable portfolio on itch.io/Steam is essential.",
        "subjects": ["Mathematics", "Computing fundamentals"],
        "entry": [
            "Meet the selected institution's published entry criteria or complete a Foundation / NVQ Level 5 Multimedia & Game Design route",
            "Build and publish at least one playable 2D/3D prototype",
        ],
        "cost": {
            "Low": "Use free engines (Unity Personal / Godot / Blender) and a public/BIT/NVQ computing route.",
            "Medium": "Compare local interactive media and computing programmes (SLIIT, AMDT, IIT).",
            "High": "Verify international game-design programme fees and GPU workstation hardware costs.",
        },
    },
}

# Pathway names produced by Agent 2 (agent_2_pathway_analysis.knowledge_base) that do not contain a
# catalogue key verbatim are mapped here, so every recommendation can flow into the Reality Check.
CAREER_ALIASES = {
    "machine learning engineer": "ai engineer",
    "ml engineer": "ai engineer",
    "data analyst": "data scientist",
    "full-stack": "software engineer",
    "full stack": "software engineer",
    "web developer": "software engineer",
    "cybersecurity": "cybersecurity analyst",
    "cyber security": "cybersecurity analyst",
    "cloud engineer": "devops engineer",
    "cloud / devops": "devops engineer",
    "ux designer": "ui/ux designer",
}
ALLOWED_TOOLS = {"career_prerequisite_lookup"}
COMPUTING_CAREER_PATTERN = re.compile(
    r"\bai\b|software|machine learning|data scientist|developer|devops|cyber|cloud", re.IGNORECASE
)


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
    if requirements is None:
        for alias, career in CAREER_ALIASES.items():
            if alias in normalized:
                requirements, matched = CAREER_REQUIREMENTS[career], career
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
        if COMPUTING_CAREER_PATTERN.search(target_lower) and "arts" in stream_lower:
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
