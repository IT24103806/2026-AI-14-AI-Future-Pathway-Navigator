from fastapi import APIRouter

from .agent import build_brief, draft_reply, match_faq, triage_request
from .schemas import (
    BriefRequest,
    BriefResponse,
    DraftReplyRequest,
    DraftReplyResponse,
    FaqMatchRequest,
    FaqMatchResponse,
    TriageRequest,
    TriageResponse,
)

router = APIRouter(prefix="/agent-5", tags=["Agent 5: Consultant Copilot"])


@router.post("/triage", response_model=TriageResponse)
def triage(payload: TriageRequest):
    """Classify, prioritise and route a student's question. Never publishes to a student."""
    return triage_request(payload)


@router.post("/brief", response_model=BriefResponse)
def brief(payload: BriefRequest):
    """Assemble the case brief a consultant reads before replying."""
    return build_brief(payload)


@router.post("/draft-reply", response_model=DraftReplyResponse)
def draft(payload: DraftReplyRequest):
    """Draft a reply for human review. The consultant sends it, not the agent."""
    return draft_reply(payload)


@router.post("/faq-match", response_model=FaqMatchResponse)
def faq_match(payload: FaqMatchRequest):
    """Suggest already-answered questions. The 'ask a human' option is always available."""
    return match_faq(payload)
