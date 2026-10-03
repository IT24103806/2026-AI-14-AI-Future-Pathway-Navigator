from typing import List, Literal, Optional

from pydantic import BaseModel, Field, field_validator

# Instruction-like input is rejected at the schema boundary, before any rule or model sees it. Same
# policy as Agent 4, kept identical on purpose so the two agents can be explained the same way.
BLOCKED_TOKENS = ("ignore previous", "system prompt", "developer message", "tool call", "<script", "disregard all")


class _GuardedRequest(BaseModel):
    @field_validator("*", mode="before")
    @classmethod
    def reject_instruction_like_input(cls, value):
        if isinstance(value, str):
            cleaned = value.strip()
            if any(token in cleaned.lower() for token in BLOCKED_TOKENS):
                raise ValueError("Instruction-like or unsafe input is not allowed")
            return cleaned
        return value


class ExecutionStep(BaseModel):
    step: str
    status: Literal["success", "failed"]
    duration_ms: float = Field(ge=0)


class TriageRequest(_GuardedRequest):
    subject: str = Field(min_length=4, max_length=140)
    body: str = Field(min_length=10, max_length=4000)
    context_type: Literal["CareerDiscovery", "PathwayPlan", "RealityCheck", "General"] = "General"
    context_summary: str = Field(default="", max_length=400)
    academic_stage: Optional[str] = Field(default=None, max_length=80)


class TriageResponse(BaseModel):
    workflow_id: str
    status: Literal["completed", "safe_failure"]
    category: Literal[
        "GuideRequest", "DoubtAnswer", "RealityCheckClarification", "PathwayAdvice", "MarketCourseInfo", "Unclassified"
    ]
    priority: Literal["P1", "P2", "P3"]
    expertise_tags: List[str]
    language: str
    sentiment: Literal["neutral", "positive", "concerned", "frustrated"]
    safety_flags: List[str]
    duplicate_of: Optional[str] = None
    suggested_sla_hours: int = Field(ge=1, le=168)
    confidence: float = Field(ge=0, le=1)
    reasoning: str
    validation_results: List[str]
    execution_trace: List[ExecutionStep]
    context_summary: str = ""
    error: Optional[str] = None


class BriefRequest(_GuardedRequest):
    subject: str = Field(min_length=4, max_length=140)
    body: str = Field(min_length=10, max_length=4000)
    context_type: str = Field(max_length=30)
    context_summary: str = Field(default="", max_length=400)
    student_evidence: str = Field(default="{}", max_length=8000)


class BriefSection(BaseModel):
    title: str
    points: List[str]


class BriefResponse(BaseModel):
    workflow_id: str
    status: Literal["completed", "safe_failure"]
    headline: str
    sections: List[BriefSection]
    evidence_sources: List[str]
    confidence: float = Field(ge=0, le=1)
    validation_results: List[str]
    execution_trace: List[ExecutionStep]
    error: Optional[str] = None


class DraftReplyRequest(_GuardedRequest):
    subject: str = Field(min_length=4, max_length=140)
    body: str = Field(min_length=10, max_length=4000)
    context_type: str = Field(max_length=30)
    context_summary: str = Field(default="", max_length=400)
    student_evidence: str = Field(default="{}", max_length=8000)
    tone: Literal["Supportive", "Direct", "Detailed"] = "Supportive"


class DraftReplyResponse(BaseModel):
    workflow_id: str
    draft_id: str
    status: Literal["completed", "safe_failure"]
    body: str
    citations: List[str]
    confidence: float = Field(ge=0, le=1)
    low_confidence: bool
    safety_notes: List[str]
    # Hard gate: the UI must show a warning and the backend records that a human wrote the reply.
    must_escalate: bool
    human_review_required: bool
    validation_results: List[str]
    execution_trace: List[ExecutionStep]
    error: Optional[str] = None


class FaqMatchRequest(_GuardedRequest):
    question: str = Field(min_length=4, max_length=500)


class FaqMatchItem(BaseModel):
    question: str
    answer: str
    score: float = Field(ge=0, le=1)


class FaqMatchResponse(BaseModel):
    workflow_id: str
    status: Literal["completed", "safe_failure"]
    matches: List[FaqMatchItem]
    human_option_available: bool = True
    validation_results: List[str]
    execution_trace: List[ExecutionStep]
    error: Optional[str] = None
