from fastapi import APIRouter, HTTPException
from .schemas import Agent1ChatRequest, Agent1ChatResponse, ExtractedProfileSlots
from .agent import run_agent_1_turn

router = APIRouter(prefix="/agent-1", tags=["Agent 1 - Conversational Onboarding"])

@router.post("/chat", response_model=Agent1ChatResponse)
async def chat_with_agent_1(request: Agent1ChatRequest):
    """
    Executes a conversational turn with Agent 1 to extract onboarding slots.
    """
    try:
        current_slots = request.current_slots or ExtractedProfileSlots()
        history_dicts = [{"role": m.role, "content": m.content} for m in request.history]
        
        response = run_agent_1_turn(
            user_message=request.message,
            history=history_dicts,
            current_slots=current_slots
        )
        return response
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Agent 1 processing failed: {str(e)}")

@router.get("/health")
async def agent_1_health():
    return {"status": "healthy", "agent": "agent_1_conversation"}
