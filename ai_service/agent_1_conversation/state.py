from typing import TypedDict, List, Optional
from langchain_core.messages import BaseMessage
from .schemas import ExtractedProfileSlots

class Agent1State(TypedDict):
    messages: List[BaseMessage]
    current_message: str
    extracted_slots: ExtractedProfileSlots
    missing_slots: List[str]
    is_complete: bool
    final_reply: Optional[str]
