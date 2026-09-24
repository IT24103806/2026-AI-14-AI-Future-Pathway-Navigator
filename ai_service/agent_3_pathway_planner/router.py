from fastapi import APIRouter, HTTPException

from .agent import run_pathway_planner
from .schemas import PlannerRequest, PlannerResponse

router = APIRouter(prefix="/agent-3", tags=["Agent 3 - Pathway Planner"])


@router.post("/plan", response_model=PlannerResponse)
async def plan_pathway(request: PlannerRequest):
    try:
        return run_pathway_planner(request)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Agent 3 processing failed: {exc}") from exc


@router.get("/health")
async def planner_health():
    return {"status": "healthy", "agent": "agent_3_pathway_planner"}