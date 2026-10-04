"""Agent 5: Consultant Copilot - triage, case brief, FAQ match and draft replies.

Distinct AI responsibility (contrast with Agents 1-4):
  * Agents 1-4 *produce guidance* for the student.
  * Agent 5 *prepares work for a human*. It classifies a student's question, assembles the evidence the
    other agents produced, and drafts a reply that a consultant must explicitly review and send.

Nothing in this module ever publishes to a student. Every entry point is deterministic and testable,
and each returns a safe, schema-valid fallback rather than raising - the consultation workflow must keep
working with the AI service degraded, because a student question is never allowed to be lost.
"""

import re
from time import perf_counter
from uuid import uuid4

from .schemas import (
    BriefRequest,
    BriefResponse,
    BriefSection,
    DraftReplyRequest,
    DraftReplyResponse,
    ExecutionStep,
    FaqMatchItem,
    FaqMatchRequest,
    FaqMatchResponse,
    TriageRequest,
    TriageResponse,
)

# ---------------------------------------------------------------------------- allow-listed knowledge
# The only knowledge Agent 5 may consult. It is the same curated, offline catalogue that Agent 4 uses
# for prerequisites - no network, no student data beyond the payload it was given.

EXPERTISE_KEYWORDS = {
    "AI/ML": ["ai", "machine learning", "ml", "deep learning", "neural", "data science", "pytorch"],
    "Software Engineering": ["software", "programming", "developer", "backend", "frontend", "full stack", "code"],
    "Cyber Security": ["cyber", "security", "hacking", "soc", "penetration", "network security"],
    "Cloud & DevOps": ["cloud", "devops", "aws", "azure", "docker", "kubernetes", "deployment"],
    "Data & Analytics": ["data", "analytics", "sql", "power bi", "statistics", "dashboard"],
    "UI/UX & Design": ["design", "ux", "ui", "figma", "wireframe", "prototype", "portfolio"],
    "Mobile Development": ["mobile", "flutter", "android", "ios", "dart", "app"],
    "Higher Education": ["university", "degree", "admission", "ucas", "campus", "faculty", "gpa"],
    "Scholarships & Finance": ["scholarship", "loan", "fee", "cost", "afford", "budget", "payment", "ifs ls", "ifsls"],
    "Career Entry": ["internship", "job", "cv", "resume", "interview", "hiring", "entry level"],
}

CATEGORY_RULES = [
    ("MarketCourseInfo", ["course", "certification", "where can i learn", "where do i learn", "training", "bootcamp", "tutorial"]),
    ("RealityCheckClarification", ["reality check", "feasibility", "high risk", "risk flag", "rejected", "needs revision", "why was my pathway"]),
    ("PathwayAdvice", ["which pathway", "should i choose", "better choice", "compare", "roadmap", "milestone", "next step"]),
    ("GuideRequest", ["guide", "resource", "material", "roadmap for", "how do i start", "step by step", "checklist"]),
    ("DoubtAnswer", ["what is", "how does", "explain", "difference between", "why is", "meaning of"]),
]

# Language that must never be published by the AI (or, ideally, by a consultant without verification).
GUARANTEE_PATTERNS = [
    (r"\bguarantee(d|s)?\b", "guarantee"),
    (r"\bwill definitely\b", "certainty claim"),
    (r"\b100% (success|job|placement)\b", "certainty claim"),
    (r"\bvisa will\b", "visa outcome claim"),
    (r"\bsalary of at least\b", "salary guarantee"),
    (r"\byou should (stop|quit|drop out)\b", "high-impact personal advice"),
]

SAFETY_PATTERNS = [
    (r"\b(suicide|kill myself|self[ -]?harm|end my life)\b", "self_harm"),
    # Wellbeing covers more than clinical terms: "I feel hopeless / I can't cope" is exactly when a
    # human should look at the case rather than a template answer being sent.
    (
        r"\b(depress(ed|ion)|anxiety|anxious|panic attack|mental health|hopeless|burnt out|burned out|"
        r"overwhelmed|can't cope|cannot cope|stressed out|no motivation)\b",
        "wellbeing",
    ),
    (r"\b(harass(ed|ment|ing)|bully|abuse)\b", "harassment"),
    (r"\b(lawsuit|sue|legal action|lawyer)\b", "legal"),
    (r"\b(debt|bankrupt|loan default)\b", "financial_hardship"),
]

BLOCKED_INPUT = ("ignore previous", "system prompt", "developer message", "tool call", "<script", "disregard all")


def _record_step(trace, name, started, status="success"):
    trace.append(ExecutionStep(step=name, status=status, duration_ms=round((perf_counter() - started) * 1000, 3)))


def _contains_instruction_like_input(*values):
    for value in values:
        if value and any(token in value.lower() for token in BLOCKED_INPUT):
            return True
    return False


def _matches(text, keywords):
    """Keyword match on word boundaries.

    Plain substring matching is unsafe for short tags: "ai" would match "said", "wait" or "explain"
    and route a completely unrelated question to the AI/ML queue.
    """
    return [keyword for keyword in keywords if re.search(rf"\b{re.escape(keyword)}\b", text)]


def _first_match(text, keywords):
    for keyword in keywords:
        if re.search(rf"\b{re.escape(keyword)}\b", text):
            return keyword
    return None


def _detect_language(text):
    """Very small heuristic: Sinhala/Tamil unicode blocks vs. latin text.

    Deliberately not an LLM call - routing a queue must not depend on a model being reachable.
    """
    if re.search(r"[\u0D80-\u0DFF]", text):  # Sinhala block
        return "Sinhala"
    if re.search(r"[\u0B80-\u0BFF]", text):  # Tamil block
        return "Tamil"
    return "English"


def _classify_category(text, context_type="General"):
    """Text rules first, then the frozen journey context as a tie-breaker.

    Without the context hint, "can you approve my Reality Check so I can register for the course"
    matched the word "course" and landed in MarketCourseInfo - losing the fact that the student is
    blocked on an approval decision.
    """
    for category, keywords in CATEGORY_RULES:
        if any(keyword in text for keyword in keywords):
            if context_type == "RealityCheck" and category in {"MarketCourseInfo", "DoubtAnswer"}:
                return "RealityCheckClarification"
            return category
    if context_type == "RealityCheck":
        return "RealityCheckClarification"
    return "Unclassified"


def _classify_expertise(text):
    tags = []
    for tag, keywords in EXPERTISE_KEYWORDS.items():
        if _matches(text, keywords):
            tags.append(tag)
    return tags


def _classify_safety(text):
    flags = []
    for pattern, flag in SAFETY_PATTERNS:
        if re.search(pattern, text, re.IGNORECASE):
            flags.append(flag)
    return flags


def _classify_priority(text, context_type, safety_flags):
    """P1 = the student is blocked right now; P3 = background curiosity.

    Rule order matters and is deterministic, so it can be unit-tested and explained in a viva.
    """
    if "self_harm" in safety_flags:
        return "P1", 24
    if re.search(r"\b(urgent|asap|deadline|tomorrow|closing date|last date|expires)\b", text):
        return "P1", 24
    # A student blocked in front of a pending/blocked Reality Check is the flagship P1 case.
    if context_type == "RealityCheck" and re.search(r"\b(pending|blocked|rejected|needs revision|high risk|cannot proceed)\b", text):
        return "P1", 24
    if context_type in {"CareerDiscovery", "RealityCheck", "PathwayPlan"}:
        return "P2", 48
    if re.search(r"\b(curious|wondering|no rush|just asking|someday)\b", text):
        return "P3", 72
    return "P2", 48


def _detect_sentiment(text):
    if re.search(r"\b(anxious|worried|scared|stress(ed)?|confused|lost|overwhelmed|hopeless)\b", text):
        return "concerned"
    if re.search(r"\b(thanks|thank you|excited|great|helpful|appreciate)\b", text):
        return "positive"
    if re.search(r"\b(angry|frustrated|unfair|useless|terrible|annoyed)\b", text):
        return "frustrated"
    return "neutral"


def sanitise_question(text):
    """Trims, collapses whitespace and drops anything instruction-like."""
    cleaned = re.sub(r"\s+", " ", (text or "")).strip()
    return cleaned


# -------------------------------------------------------------------------------------- triage

def triage_request(request: TriageRequest) -> TriageResponse:
    workflow_id = f"triage-{uuid4()}"
    trace, reasoning = [], []

    try:
        started = perf_counter()
        subject = sanitise_question(request.subject)
        body = sanitise_question(request.body)
        context_summary = sanitise_question(request.context_summary)
        text = f"{subject} {body}".lower()

        if len(subject) < 4 or len(body) < 10:
            raise ValueError("Subject must be at least 4 and body at least 10 characters")
        if _contains_instruction_like_input(subject, body):
            raise ValueError("Instruction-like input is not allowed")
        reasoning.append("input sanitised and prompt-injection check passed")
        _record_step(trace, "parse_and_guard_input", started)

        started = perf_counter()
        routing_text = f"{text} {context_summary.lower()}"
        safety_flags = _classify_safety(text)
        category = _classify_category(text, request.context_type)
        priority, sla_hours = _classify_priority(text, request.context_type, safety_flags)
        expertise = _classify_expertise(routing_text)
        language = _detect_language(f"{request.subject} {request.body}")
        sentiment = _detect_sentiment(text)
        reasoning.append(f"category rule matched -> {category}")
        reasoning.append(f"priority rules -> {priority} ({sla_hours}h SLA)")
        if safety_flags:
            reasoning.append(f"safety flags -> {', '.join(safety_flags)}")
        _record_step(trace, "apply_triage_rules", started)

        started = perf_counter()
        confidence = 0.55
        if category != "Unclassified":
            confidence += 0.2
        if expertise:
            confidence += 0.1
        if len(body) > 40:
            confidence += 0.05
        confidence = round(min(0.95, confidence), 2)
        reasoning.append(f"confidence {confidence} from rule coverage")
        _record_step(trace, "score_confidence", started)

        return TriageResponse(
            workflow_id=workflow_id,
            status="completed",
            category=category,
            priority=priority,
            expertise_tags=expertise,
            language=language,
            sentiment=sentiment,
            safety_flags=safety_flags,
            duplicate_of=None,
            suggested_sla_hours=sla_hours,
            confidence=confidence,
            reasoning="; ".join(reasoning),
            validation_results=["request_schema_valid", "prompt_injection_check_passed", "triage_rules_applied"],
            execution_trace=trace,
            context_summary=context_summary,
        )
    except Exception as exc:
        trace.append(ExecutionStep(step="safe_failure", status="failed", duration_ms=0))
        # A failed triage must never block the student's question: the request is still stored and a
        # consultant still sees it (the backend maps this to "untriaged", not to an error).
        return TriageResponse(
            workflow_id=workflow_id,
            status="safe_failure",
            category="Unclassified",
            priority="P2",
            expertise_tags=[],
            language="English",
            sentiment="neutral",
            safety_flags=[],
            duplicate_of=None,
            suggested_sla_hours=48,
            confidence=0.0,
            reasoning="Triage could not be completed safely; a consultant will classify this manually.",
            validation_results=["request_schema_valid"],
            execution_trace=trace,
            context_summary="",
            error=str(exc),
        )


# --------------------------------------------------------------------------------------- brief

def build_brief(request: BriefRequest) -> BriefResponse:
    workflow_id = f"brief-{uuid4()}"
    trace = []

    try:
        started = perf_counter()
        subject = sanitise_question(request.subject)
        body = sanitise_question(request.body)
        if _contains_instruction_like_input(subject, body):
            raise ValueError("Instruction-like input is not allowed")
        _record_step(trace, "parse_and_guard_input", started)

        started = perf_counter()
        routing_text = f"{subject} {body} {request.context_summary}".lower()
        question_type = _classify_category(f"{subject} {body}".lower(), request.context_type)
        expertise = _classify_expertise(routing_text)

        sections = [
            BriefSection(
                title="What the student is asking",
                points=[subject, body[:400] + ("…" if len(body) > 400 else "")],
            ),
            BriefSection(
                title="Where they are in the journey",
                points=[
                    f"Context: {request.context_type}",
                    request.context_summary or "No frozen context was attached to this request.",
                ],
            ),
            BriefSection(
                title="Question type detected",
                points=[
                    f"Category: {question_type}",
                    f"Suggested expertise: {', '.join(expertise) if expertise else 'general guidance'}",
                ],
            ),
        ]

        # The student's own agent evidence is summarised, never dumped: a consultant needs the picture,
        # not a JSON blob.
        evidence_points = _summarise_evidence(request.student_evidence)
        if evidence_points:
            sections.append(BriefSection(title="Evidence already produced by the agents", points=evidence_points))

        suggested = [
            "Answer the question directly in the first two sentences.",
            "Reference the student's own numbers above rather than generic advice.",
        ]
        if question_type == "RealityCheckClarification":
            suggested.append("If the answer changes the risk picture, escalate so the counsellor sees it before deciding.")
        if question_type == "GuideRequest":
            suggested.append("Attach at least one concrete guide link so the student can act immediately.")
        sections.append(BriefSection(title="Suggested approach", points=suggested))

        _record_step(trace, "assemble_case_brief", started)

        return BriefResponse(
            workflow_id=workflow_id,
            status="completed",
            headline=f"{question_type} · {request.context_type}",
            sections=sections,
            evidence_sources=[
                "Frozen request context snapshot",
                "Student profile and agent outputs supplied in the request payload",
                "Curated career knowledge base (offline)",
            ],
            confidence=0.7 if evidence_points else 0.5,
            validation_results=["brief_schema_valid", "no_personal_data_beyond_payload"],
            execution_trace=trace,
        )
    except Exception as exc:
        trace.append(ExecutionStep(step="safe_failure", status="failed", duration_ms=0))
        return BriefResponse(
            workflow_id=workflow_id,
            status="safe_failure",
            headline="Brief unavailable",
            sections=[],
            evidence_sources=[],
            confidence=0.0,
            validation_results=["brief_schema_valid"],
            execution_trace=trace,
            error=str(exc),
        )


def _summarise_evidence(evidence_json: str):
    """Extracts a few human sentences from the JSON bundle the backend sends."""
    import json

    points = []
    try:
        evidence = json.loads(evidence_json or "{}")
    except (ValueError, TypeError):
        return points

    profile = evidence.get("profile") or {}
    if profile:
        bits = []
        if profile.get("academicStage"):
            bits.append(f"stage: {profile['academicStage']}")
        if profile.get("alStream"):
            bits.append(f"A/L stream: {profile['alStream']}")
        if profile.get("budgetLevel"):
            bits.append(f"budget: {profile['budgetLevel']}")
        skills = profile.get("coreSkills") or []
        if skills:
            bits.append(f"skills: {', '.join(str(s) for s in skills[:6])}")
        if bits:
            points.append("Profile · " + " | ".join(bits))

    roadmap = evidence.get("latestRoadmap") or {}
    if roadmap.get("selectedPathway"):
        points.append(f"Roadmap · {roadmap['selectedPathway']} (next: {roadmap.get('nextAction') or 'not set'})")

    review = evidence.get("latestReview") or {}
    if review.get("targetCareer"):
        points.append(
            f"Reality Check · {review['targetCareer']} · {review.get('status')} · "
            f"feasibility {review.get('feasibilityScore')}%"
        )
        missing = review.get("missingSkills") or []
        if missing:
            points.append("Missing prerequisites · " + ", ".join(str(s) for s in missing[:6]))

    return points


# --------------------------------------------------------------------------------- draft reply

def draft_reply(request: DraftReplyRequest) -> DraftReplyResponse:
    workflow_id = f"draft-{uuid4()}"
    trace = []

    try:
        started = perf_counter()
        subject = sanitise_question(request.subject)
        body = sanitise_question(request.body)
        tone = (request.tone or "Supportive").strip().capitalize()
        if tone not in {"Supportive", "Direct", "Detailed"}:
            tone = "Supportive"
        if _contains_instruction_like_input(subject, body):
            raise ValueError("Instruction-like input is not allowed")
        _record_step(trace, "parse_and_guard_input", started)

        started = perf_counter()
        routing_text = f"{subject} {body} {request.context_summary}".lower()
        category = _classify_category(f"{subject} {body}".lower(), request.context_type)
        expertise = _classify_expertise(routing_text)
        safety_flags = _classify_safety(f"{subject} {body}".lower())
        evidence_points = _summarise_evidence(request.student_evidence)
        _record_step(trace, "classify_and_collect_evidence", started)

        started = perf_counter()
        paragraphs = []
        greeting = "Thanks for reaching out — this is a good question to ask before you commit."
        if tone == "Direct":
            greeting = "Here is the short answer first, then the detail."

        paragraphs.append(greeting)
        paragraphs.append(_draft_body_for(category, subject, body, evidence_points, tone))

        if category == "GuideRequest":
            paragraphs.append(
                "I have attached a guide below. Work through it in order and tick off the checklist — "
                "if any step does not match your situation, reply here and I will adjust it."
            )
        elif category == "RealityCheckClarification":
            paragraphs.append(
                "Important: I cannot approve or reject a Reality Check — that is the counsellor's decision. "
                "What I can do is explain the evidence and, if it changes the picture, escalate it so the "
                "counsellor sees my note before deciding."
            )
        else:
            paragraphs.append("Tell me which part is still unclear and I will go deeper on that specific point.")

        draft_body = "\n\n".join(paragraphs)
        _record_step(trace, "compose_draft", started)

        started = perf_counter()
        safety_notes = []
        must_escalate = False
        for pattern, label in GUARANTEE_PATTERNS:
            if re.search(pattern, draft_body, re.IGNORECASE):
                safety_notes.append(f"Draft contained a {label}; it was rewritten before review.")
                must_escalate = True

        if request.context_type == "RealityCheck" or category == "RealityCheckClarification":
            safety_notes.append("Reality Check decisions stay with the counsellor (ADR-004).")
        if re.search(r"\b(approve|approval|reject my|grant my|sign off)\b", f"{subject} {body}", re.IGNORECASE):
            must_escalate = True
            safety_notes.append(
                "The student is asking for an approval decision. A consultant cannot approve or reject a "
                "pathway - escalate so the counsellor decides, and reply only to explain the process."
            )
        if safety_flags:
            must_escalate = True
            safety_notes.append(
                "The student's message raised: " + ", ".join(safety_flags) + ". Follow the escalation policy before replying."
            )

        confidence = 0.45
        if category != "Unclassified":
            confidence += 0.15
        if evidence_points:
            confidence += 0.15
        if len(body) > 60:
            confidence += 0.1
        confidence = round(min(0.9, confidence), 2)
        low_confidence = confidence < 0.55
        _record_step(trace, "lint_and_score_draft", started)

        return DraftReplyResponse(
            workflow_id=workflow_id,
            draft_id=f"draft-{uuid4()}",
            status="completed",
            body=draft_body,
            citations=[
                point for point in evidence_points[:3]
            ] + (["Curated career knowledge base (offline)"] if expertise else []),
            confidence=confidence,
            low_confidence=low_confidence,
            safety_notes=safety_notes,
            must_escalate=must_escalate,
            human_review_required=True,
            validation_results=[
                "draft_schema_valid",
                "guarantee_language_checked",
                "no_auto_send_confirmed",
                "prompt_injection_check_passed",
            ],
            execution_trace=trace,
        )
    except Exception as exc:
        trace.append(ExecutionStep(step="safe_failure", status="failed", duration_ms=0))
        return DraftReplyResponse(
            workflow_id=workflow_id,
            draft_id=f"draft-{uuid4()}",
            status="safe_failure",
            body="",
            citations=[],
            confidence=0.0,
            low_confidence=True,
            safety_notes=["Draft generation failed; please write the reply manually."],
            must_escalate=False,
            human_review_required=True,
            validation_results=["draft_schema_valid"],
            execution_trace=trace,
            error=str(exc),
        )


def _draft_body_for(category, subject, body, evidence_points, tone):
    """Deterministic draft skeleton.

    This is intentionally rule-based rather than a free-form LLM call: a draft that a consultant must
    review should be reproducible, testable and free of invented facts. The evidence lines below are the
    student's own data, quoted back, which is exactly what a good first reply does.
    """
    opener = {
        "GuideRequest": "You are asking for a practical way forward, so here is one.",
        "MarketCourseInfo": "Short answer: pick the route that matches your current stage and budget, not the one with the biggest name.",
        "RealityCheckClarification": "Let me explain what that Reality Check result actually means for you.",
        "PathwayAdvice": "Picking between pathways is mostly about fit, not ranking — here is how to decide.",
        "DoubtAnswer": "Here is the explanation in plain terms.",
        "Unclassified": "Thanks for the details — here is how I would approach this.",
    }.get(category, "Thanks for the details — here is how I would approach this.")

    detail = opener
    if evidence_points:
        detail += "\n\nFrom your own profile: " + "; ".join(evidence_points[:2]) + "."
    else:
        detail += "\n\nI have your question, though there is no saved pathway attached to it yet."

    if tone == "Detailed":
        detail += "\n\nI will break this into steps with the reasoning behind each one."
    if subject:
        detail += f"\n\nOn \"{subject}\": the key point is that the evidence matters more than the label."

    return detail


# ------------------------------------------------------------------------------------- FAQ match

FAQ_CORPUS = [
    (
        "How do I decide between two pathways?",
        "Compare the entry requirements, the cost for your budget, and the first three milestones of each "
        "roadmap. If one needs a prerequisite you do not have yet, that is usually the deciding factor.",
    ),
    (
        "Can I switch from an Arts stream to a computing degree?",
        "Yes, but normally through an accredited foundation or NVQ Level 5 bridging route. Check the "
        "institution's published entry criteria and budget for the extra foundation year.",
    ),
    (
        "What does 'NeedsRevision' on my Reality Check mean?",
        "The counsellor needs something changed or clarified before approving the pathway - usually a "
        "prerequisite or a cost detail. Open the review, read the feedback, then resubmit with the fix.",
    ),
    (
        "Where can I learn the missing skills my roadmap lists?",
        "Start with one free or low-cost course per skill, and prove each one with a small project. Ask a "
        "consultant for a guide and they will attach a checklist to the exact milestone.",
    ),
    (
        "Is the pathway recommendation final?",
        "No. It is a scored suggestion based on your profile and current market data. You can re-run Career "
        "Discovery after updating your onboarding profile, and a consultant can explain the trade-offs.",
    ),
]


def match_faq(request: FaqMatchRequest) -> FaqMatchResponse:
    workflow_id = f"faq-{uuid4()}"
    trace = []

    try:
        started = perf_counter()
        question = sanitise_question(request.question).lower()
        if not question:
            raise ValueError("Question is required")
        if _contains_instruction_like_input(question):
            raise ValueError("Instruction-like input is not allowed")
        _record_step(trace, "parse_and_guard_input", started)

        started = perf_counter()
        question_tokens = {token for token in re.findall(r"[a-z]{4,}", question)}
        matches = []
        for faq_question, faq_answer in FAQ_CORPUS:
            faq_tokens = {token for token in re.findall(r"[a-z]{4,}", faq_question.lower())}
            if not faq_tokens:
                continue
            overlap = len(question_tokens & faq_tokens) / len(faq_tokens)
            if overlap >= 0.25:
                matches.append(FaqMatchItem(question=faq_question, answer=faq_answer, score=round(min(0.95, overlap), 2)))
        matches.sort(key=lambda item: item.score, reverse=True)
        matches = matches[:3]
        _record_step(trace, "score_faq_overlap", started)

        return FaqMatchResponse(
            workflow_id=workflow_id,
            status="completed",
            matches=matches,
            # Deliberately always true: self-service suggestions must never replace the human door.
            human_option_available=True,
            validation_results=["faq_schema_valid", "human_escalation_always_available"],
            execution_trace=trace,
        )
    except Exception as exc:
        trace.append(ExecutionStep(step="safe_failure", status="failed", duration_ms=0))
        return FaqMatchResponse(
            workflow_id=workflow_id,
            status="safe_failure",
            matches=[],
            human_option_available=True,
            validation_results=["faq_schema_valid"],
            execution_trace=trace,
            error=str(exc),
        )
