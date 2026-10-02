from typing import List, Optional, Literal
from pydantic import BaseModel, Field

class ExtractedProfileSlots(BaseModel):
    full_name: Optional[str] = Field(
        default=None,
        description="Student's display name, only when stated explicitly (e.g. 'Nimal Perera'). Never guess it."
    )
    academic_stage: Optional[str] = Field(
        default=None,
        description="Academic background (e.g. 'After O/L', 'After A/L', 'Undergraduate', 'Graduate', 'Other')"
    )
    core_skills: List[str] = Field(
        default_factory=list,
        description="Technical, creative, or soft skills the student mentions (e.g. ['Python', 'Problem Solving', 'Graphic Design'])"
    )
    hobbies_interests: List[str] = Field(
        default_factory=list,
        description="Hobbies, passion projects, or areas of interest (e.g. ['Robotics', 'Gaming', 'Writing', 'AI'])"
    )
    career_ambitions: Optional[str] = Field(
        default=None,
        description="Future career goals, dream roles, or industries (e.g. 'Software Engineer', 'Data Scientist', 'Cybersecurity Analyst')"
    )

class ChatMessage(BaseModel):
    role: Literal["user", "assistant", "system"] = Field(..., description="Role of the speaker")
    content: str = Field(..., description="Message text content")

class Agent1ChatRequest(BaseModel):
    user_id: Optional[str] = Field(default=None, description="Unique User ID from .NET JWT")
    message: str = Field(..., description="Current user input text")
    history: List[ChatMessage] = Field(default_factory=list, description="Previous conversational history")
    current_slots: Optional[ExtractedProfileSlots] = Field(default=None, description="Slots already extracted in prior turns")
    baseline_slots: Optional[ExtractedProfileSlots] = Field(
        default=None,
        description="Profile as stored before this session; lets update sessions detect real changes"
    )
    update_mode: bool = Field(
        default=False,
        description="True when the student is revisiting an existing profile to refresh it"
    )

class Agent1ChatResponse(BaseModel):
    reply_message: str = Field(..., description="Agent 1's conversational response")
    extracted_slots: ExtractedProfileSlots = Field(..., description="Accumulated extracted slots")
    missing_slots: List[str] = Field(default_factory=list, description="List of missing slot names required for completion")
    is_complete: bool = Field(default=False, description="True if all critical slots meet validation thresholds")
