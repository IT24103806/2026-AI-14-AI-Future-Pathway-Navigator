from typing import List, Literal, Optional

from pydantic import BaseModel, Field, field_validator


class RealityCheckRequest(BaseModel):
    student_id: str = Field(min_length=1, max_length=64)
    pathway_analysis_id: Optional[str] = Field(default=None, max_length=64)
    target_career: str = Field(min_length=2, max_length=120)
    al_stream: str = Field(min_length=2, max_length=80)
    al_results: str = Field(min_length=1, max_length=80)
    budget_level: Literal["Low", "Medium", "High"]
    current_skills: List[str] = Field(default_factory=list, max_length=30)

    @field_validator("student_id", "pathway_analysis_id", "target_career", "al_stream", "al_results")
    @classmethod
    def reject_instruction_like_input(cls, value: Optional[str]) -> Optional[str]:
        if value is None:
            return value
        cleaned = value.strip()
        blocked = ("ignore previous", "system prompt", "developer message", "tool call", "<script")
        if any(token in cleaned.lower() for token in blocked):
            raise ValueError("Instruction-like or unsafe input is not allowed")
        return cleaned

    @field_validator("current_skills")
    @classmethod
    def clean_skills(cls, values: List[str]) -> List[str]:
        cleaned = []
        for value in values:
            skill = value.strip()
            if not skill or len(skill) > 80:
                raise ValueError("Each skill must contain 1-80 characters")
            if skill.lower() not in {item.lower() for item in cleaned}:
                cleaned.append(skill)
        return cleaned


class ToolCallSummary(BaseModel):
    tool_name: str
    status: Literal["success", "rejected", "error"]
    duration_ms: float = Field(ge=0)
    result_summary: str


class ExecutionStep(BaseModel):
    step: str
    status: Literal["success", "failed"]
    duration_ms: float = Field(ge=0)


class RealityCheckResponse(BaseModel):
    workflow_id: str
    status: Literal["completed", "approval_required", "safe_failure"]
    is_feasible: bool
    feasibility_score: int = Field(ge=0, le=100)
    missing_skills: List[str]
    skill_gap_summary: str
    degree_requirement: str
    subject_requirements: List[str]
    entry_requirements: List[str]
    cost_guidance: str
    gap_closure_plan: List[str]
    evidence_sources: List[str]
    is_high_risk: bool
    risk_reason: Optional[str] = None
    counsellor_review_required: bool
    suggested_action: str
    validation_results: List[str]
    tool_calls: List[ToolCallSummary]
    execution_trace: List[ExecutionStep]
    error: Optional[str] = None
