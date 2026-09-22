from fastapi import APIRouter
from .schemas import RealityCheckRequest, RealityCheckResponse
from .agent import evaluate_reality_check

router = APIRouter(prefix="/agent-4", tags=["Agent 4: Reality Check"])

@router.post("/evaluate", response_model=RealityCheckResponse)
def evaluate_pathway(payload: RealityCheckRequest):
    return evaluate_reality_check(payload)