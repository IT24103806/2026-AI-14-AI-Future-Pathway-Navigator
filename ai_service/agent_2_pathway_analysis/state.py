from typing import TypedDict, List, Optional
from .schemas import StudentProfileInput, CareerCandidate, CareerPathRecommendation


class Agent2State(TypedDict):
    workflow_id: str
    profile: StudentProfileInput
    plan: List[str]                              # explicit multi-step plan (for observability/audit)
    candidates: List[CareerCandidate]             # after matching + market data tool calls
    recommendations: List[CareerPathRecommendation]  # after LLM reasoning
    validation_errors: List[str]
    is_valid: bool
    status: Optional[str]                         # "pending_approval" | "failed"
    execution_trace: List[dict]                   # [{node, duration_ms, status}] per node run
