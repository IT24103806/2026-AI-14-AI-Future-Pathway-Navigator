from typing import List, Literal, Optional

from pydantic import BaseModel, Field


class PlannerProfile(BaseModel):
    academic_stage: Optional[str] = None
    core_skills: List[str] = Field(default_factory=list)
    career_ambitions: Optional[str] = None


class PlannerRequest(BaseModel):
    user_id: Optional[str] = None
    selected_pathway: str = Field(..., min_length=2)
    profile: PlannerProfile
    completed_phases: List[str] = Field(default_factory=list)


class RoadmapStage(BaseModel):
    order: int = Field(..., ge=1)
    stage: Literal["education", "skills", "certification", "internship", "first_job", "long_term_growth"]
    title: str
    outcome: str
    actions: List[str] = Field(default_factory=list)
    estimated_duration: str
    status: Literal["not_started", "in_progress", "completed"] = "not_started"


class PlannerResponse(BaseModel):
    workflow_id: str
    status: Literal["ready", "failed"]
    selected_pathway: str
    roadmap: List[RoadmapStage] = Field(default_factory=list)
    missing_skills: List[str] = Field(default_factory=list)
    next_action: str = ""
    validation_errors: List[str] = Field(default_factory=list)
    execution_trace: List[dict] = Field(default_factory=list)