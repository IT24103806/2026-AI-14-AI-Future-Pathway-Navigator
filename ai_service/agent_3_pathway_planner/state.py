from typing import Any, Dict, List, Optional, TypedDict

from .schemas import PlannerRequest, RoadmapStage


class PlannerState(TypedDict):
    request: PlannerRequest
    pathway: Optional[dict]
    roadmap: List[RoadmapStage]
    missing_skills: List[str]
    next_action: str
    validation_errors: List[str]
    status: str
    execution_trace: List[Dict[str, Any]]