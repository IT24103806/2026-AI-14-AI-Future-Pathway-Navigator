from typing import List, Optional, Literal
from pydantic import BaseModel, Field


class StudentProfileInput(BaseModel):
    """Completed profile handed off from Agent 1 / the student's saved profile."""
    academic_stage: Optional[str] = None
    core_skills: List[str] = Field(default_factory=list)
    hobbies_interests: List[str] = Field(default_factory=list)
    career_ambitions: Optional[str] = None


class MarketData(BaseModel):
    """Real-world demand/competition signal for one candidate career."""
    demand_score: int = Field(..., ge=0, le=100, description="0-100, higher = more openings/demand")
    competition_score: int = Field(..., ge=0, le=100, description="0-100, higher = more applicants per opening")
    trend: Literal["rising", "stable", "declining"] = Field(
        ..., description="Posting-rate direction: recent window vs a longer baseline window"
    )
    sample_size: int = Field(..., ge=0, description="Number of job postings the score was derived from")
    data_source: Literal["adzuna_live", "simulated_fallback"] = "simulated_fallback"
    region: str = "lk"


class RoadmapStep(BaseModel):
    """One ordered phase in the learning roadmap for a pathway."""
    phase: str = Field(..., description="e.g. 'Foundation', 'Core Skills', 'Specialization'")
    course: str


class CareerCandidate(BaseModel):
    """Output of the matching/tool step, before LLM reasoning is added."""
    pathway_name: str
    match_score: int = Field(..., ge=0, le=100)
    matched_skills: List[str] = Field(default_factory=list)
    matched_interests: List[str] = Field(default_factory=list)
    missing_skills: List[str] = Field(default_factory=list, description="Required skills the student doesn't have yet")
    search_term: str
    market_data: Optional[MarketData] = None


class CareerPathRecommendation(BaseModel):
    """Final, explained recommendation shown to the student/advisor. 'Path A/B/C'."""
    label: str = Field(..., description="e.g. 'Path A', 'Path B', 'Path C'")
    pathway_name: str
    match_score: int = Field(..., ge=0, le=100)
    demand_score: int = Field(..., ge=0, le=100)
    competition_score: int = Field(..., ge=0, le=100)
    trend: Literal["rising", "stable", "declining"]
    data_source: Literal["adzuna_live", "simulated_fallback"]
    reasoning: str
    missing_skills: List[str] = Field(default_factory=list)
    recommended_courses: List[str] = Field(default_factory=list)
    roadmap: List[RoadmapStep] = Field(default_factory=list, description="Ordered learning phases for this pathway")


class Agent2AnalysisRequest(BaseModel):
    user_id: Optional[str] = None
    profile: StudentProfileInput


class ExecutionTraceEntry(BaseModel):
    """One node's timing/outcome — observability evidence for the graph run."""
    node: str
    duration_ms: float
    status: Literal["success", "error"]


class Agent2AnalysisResponse(BaseModel):
    workflow_id: str
    status: Literal["pending_approval", "failed"]
    recommendations: List[CareerPathRecommendation] = Field(default_factory=list)
    validation_errors: List[str] = Field(default_factory=list)
    execution_trace: List[ExecutionTraceEntry] = Field(default_factory=list)
