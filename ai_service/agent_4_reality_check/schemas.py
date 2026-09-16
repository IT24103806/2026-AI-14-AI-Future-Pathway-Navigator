from pydantic import BaseModel
from typing import List, Optional

class RealityCheckRequest(BaseModel):
    student_id: str
    target_career: str
    al_stream: str
    al_results: str
    budget_level: str
    current_skills: List[str]

class RealityCheckResponse(BaseModel):
    is_feasible: bool
    feasibility_score: int
    missing_skills: List[str]
    skill_gap_summary: str
    is_high_risk: bool
    risk_reason: Optional[str] = None
    counsellor_review_required: bool
    suggested_action: str