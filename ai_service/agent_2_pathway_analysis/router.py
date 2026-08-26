from fastapi import APIRouter, HTTPException
from .schemas import Agent2AnalysisRequest, Agent2AnalysisResponse
from .agent import run_agent_2_analysis

router = APIRouter(prefix="/agent-2", tags=["Agent 2 - Career Discovery"])


@router.post("/analyze", response_model=Agent2AnalysisResponse)
async def analyze_career_paths(request: Agent2AnalysisRequest):
    """
    Runs the full Career Discovery workflow: plan -> match candidates ->
    fetch real market data -> LLM reasoning -> validate -> pending_approval.
    """
    try:
        return run_agent_2_analysis(request.profile)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Agent 2 processing failed: {str(e)}")


@router.get("/health")
async def agent_2_health():
    return {"status": "healthy", "agent": "agent_2_career_discovery"}
