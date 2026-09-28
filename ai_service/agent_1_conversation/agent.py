import json
import re
from typing import Dict, Any, List
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage
from langgraph.graph import StateGraph, END

from core.config import settings
from .schemas import ExtractedProfileSlots, Agent1ChatResponse
from .state import Agent1State
from .prompts import SYSTEM_PROMPT_AGENT_1, EXTRACTION_SYSTEM_PROMPT

def get_llm():
    """
    Initializes the configured LLM provider (Google Gemini or OpenAI).
    Falls back gracefully if keys are not set.
    """
    if settings.LLM_PROVIDER == "google" and settings.GOOGLE_API_KEY:
        try:
            from langchain_google_genai import ChatGoogleGenerativeAI
            return ChatGoogleGenerativeAI(
                model=settings.GOOGLE_MODEL,
                google_api_key=settings.GOOGLE_API_KEY,
                temperature=0.4,
            )
        except Exception as e:
            print(f"Error initializing Google Gemini LLM: {e}")
    elif settings.LLM_PROVIDER == "openai" and settings.OPENAI_API_KEY:
        try:
            from langchain_openai import ChatOpenAI
            return ChatOpenAI(
                model=settings.OPENAI_MODEL,
                openai_api_key=settings.OPENAI_API_KEY,
                temperature=0.4,
            )
        except Exception as e:
            print(f"Error initializing OpenAI LLM: {e}")
    return None

def fallback_extract_slots(user_text: str, current_slots: ExtractedProfileSlots) -> ExtractedProfileSlots:
    """
    Rule-based heuristic extractor when LLM key is not yet configured,
    ensuring robust offline/development execution.
    """
    text_lower = user_text.lower()
    slots = current_slots.model_copy()

    # Academic Stage
    if "o/l" in text_lower or "ordinary level" in text_lower:
        slots.academic_stage = "After O/L"
    elif "a/l" in text_lower or "advanced level" in text_lower:
        slots.academic_stage = "After A/L"
    elif "undergraduate" in text_lower or "bachelor" in text_lower or "degree" in text_lower or "university" in text_lower or "sliit" in text_lower:
        slots.academic_stage = "Undergraduate"
    elif "graduat" in text_lower or "postgraduate" in text_lower or "master" in text_lower:
        slots.academic_stage = "Graduated"

    # Core Skills
    skill_keywords = [
        "python", "java", "c++", "c#", "javascript", "react", "html", "css", "sql",
        "machine learning", "deep learning", "data analysis", "problem solving",
        "graphic design", "figma", "ui/ux", "communication", "leadership", "git"
    ]
    for skill in skill_keywords:
        if skill in text_lower and not any(skill.lower() == s.lower() for s in slots.core_skills):
            slots.core_skills.append(skill.title() if len(skill) > 3 else skill.upper())

    # Hobbies / Interests
    hobby_keywords = [
        "robotics", "gaming", "ai", "artificial intelligence", "music", "reading",
        "writing", "drawing", "photography", "chess", "coding", "web development",
        "cyber security", "cloud", "iot", "open source"
    ]
    for hobby in hobby_keywords:
        if hobby in text_lower and not any(hobby.lower() == h.lower() for h in slots.hobbies_interests):
            slots.hobbies_interests.append(hobby.title() if len(hobby) > 3 else hobby.upper())

    # Career Ambitions
    career_patterns = [
        r"(?:want to be|become|aim to be|aspire to be|career as|work as a?|interested in becoming)\s+([a-zA-Z\s]+)",
        r"(?:dream job is|goal is)\s+([a-zA-Z\s]+)"
    ]
    for pattern in career_patterns:
        match = re.search(pattern, text_lower)
        if match:
            ambition = match.group(1).strip().split(".")[0].split(",")[0]
            ambition = re.sub(r"^(?:a|an)\s+", "", ambition, flags=re.IGNORECASE)
            if len(ambition) > 2:
                normalized_ambition = ambition.title()
                normalized_ambition = re.sub(r"\bAi\b", "AI", normalized_ambition)
                normalized_ambition = re.sub(r"\bUi/Ux\b", "UI/UX", normalized_ambition)
                slots.career_ambitions = normalized_ambition
                break

    if not slots.career_ambitions and ("software engineer" in text_lower or "data scientist" in text_lower or "ai engineer" in text_lower or "web developer" in text_lower):
        if "software engineer" in text_lower:
            slots.career_ambitions = "Software Engineer"
        elif "data scientist" in text_lower:
            slots.career_ambitions = "Data Scientist"
        elif "ai engineer" in text_lower:
            slots.career_ambitions = "AI Engineer"
        elif "web developer" in text_lower:
            slots.career_ambitions = "Web Developer"

    return slots

# ----------------- LangGraph Node Definitions -----------------

def extraction_node(state: Agent1State) -> Dict[str, Any]:
    """Node 1: Extract profile slots from current user input and history."""
    current_message = state["current_message"]
    existing_slots = state["extracted_slots"]
    llm = get_llm()

    if llm is None:
        updated_slots = fallback_extract_slots(current_message, existing_slots)
        return {"extracted_slots": updated_slots}

    try:
        structured_llm = llm.with_structured_output(ExtractedProfileSlots)
        prompt = [
            SystemMessage(content=EXTRACTION_SYSTEM_PROMPT),
            HumanMessage(
                content=f"Existing Extracted Slots:\n{existing_slots.model_dump_json()}\n\nLatest User Message:\n\"{current_message}\""
            )
        ]
        extracted: ExtractedProfileSlots = structured_llm.invoke(prompt)
        
        # Merge lists and retain non-null values
        merged_skills = list(set(existing_slots.core_skills + (extracted.core_skills or [])))
        merged_interests = list(set(existing_slots.hobbies_interests + (extracted.hobbies_interests or [])))
        
        merged_slots = ExtractedProfileSlots(
            academic_stage=extracted.academic_stage or existing_slots.academic_stage,
            core_skills=merged_skills,
            hobbies_interests=merged_interests,
            career_ambitions=extracted.career_ambitions or existing_slots.career_ambitions
        )
        return {"extracted_slots": merged_slots}
    except Exception as e:
        print(f"LLM extraction error, using fallback: {e}")
        updated_slots = fallback_extract_slots(current_message, existing_slots)
        return {"extracted_slots": updated_slots}

def evaluation_node(state: Agent1State) -> Dict[str, Any]:
    """Node 2: Evaluate missing slots and determine completeness."""
    slots = state["extracted_slots"]
    missing = []

    if not slots.academic_stage:
        missing.append("academic_stage (e.g. After O/L, After A/L, Undergraduate, etc.)")
    if not slots.core_skills or len(slots.core_skills) == 0:
        missing.append("core_skills (e.g. Python, Problem Solving, Mathematics, UI/UX, etc.)")
    if not slots.hobbies_interests or len(slots.hobbies_interests) == 0:
        missing.append("hobbies_interests (e.g. Robotics, Gaming, AI, Drawing, etc.)")
    if not slots.career_ambitions or len(slots.career_ambitions.strip()) == 0:
        missing.append("career_ambitions (e.g. AI Engineer, Software Architect, Data Scientist, etc.)")

    is_complete = len(missing) == 0
    return {
        "missing_slots": missing,
        "is_complete": is_complete
    }

def generation_node(state: Agent1State) -> Dict[str, Any]:
    """Node 3: Generate empathetic, concise, and targeted follow-up dialogue."""
    slots = state["extracted_slots"]
    missing = state["missing_slots"]
    is_complete = state["is_complete"]
    current_message = state["current_message"]
    messages = state["messages"]
    llm = get_llm()

    if is_complete:
        if llm:
            try:
                system_content = SYSTEM_PROMPT_AGENT_1.format(
                    academic_stage=slots.academic_stage,
                    core_skills=", ".join(slots.core_skills),
                    hobbies_interests=", ".join(slots.hobbies_interests),
                    career_ambitions=slots.career_ambitions,
                    missing_slots="NONE (Profile Complete!)"
                )
                prompt = [
                    SystemMessage(content=system_content),
                    *messages,
                    HumanMessage(content=current_message)
                ]
                ai_resp = llm.invoke(prompt)
                return {"final_reply": ai_resp.content}
            except Exception as e:
                print(f"Generation error: {e}")
        
        reply = (
            f"Awesome work! 🎉 I have gathered everything we need:\n"
            f"• **Academic Stage:** {slots.academic_stage}\n"
            f"• **Core Skills:** {', '.join(slots.core_skills)}\n"
            f"• **Interests:** {', '.join(slots.hobbies_interests)}\n"
            f"• **Career Ambition:** {slots.career_ambitions}\n\n"
            f"Your personalized AI Pathway Navigator dashboard is now ready! Redirecting you now..."
        )
        return {"final_reply": reply}

    # If missing fields remain
    if llm:
        try:
            system_content = SYSTEM_PROMPT_AGENT_1.format(
                academic_stage=slots.academic_stage or "Not yet specified",
                core_skills=", ".join(slots.core_skills) if slots.core_skills else "Not yet specified",
                hobbies_interests=", ".join(slots.hobbies_interests) if slots.hobbies_interests else "Not yet specified",
                career_ambitions=slots.career_ambitions or "Not yet specified",
                missing_slots="\n".join([f"- {m}" for m in missing])
            )
            prompt = [
                SystemMessage(content=system_content),
                *messages,
                HumanMessage(content=current_message)
            ]
            ai_resp = llm.invoke(prompt)
            return {"final_reply": ai_resp.content}
        except Exception as e:
            print(f"LLM Generation error: {e}")

    # Heuristic template fallback
    reply_parts = []
    if "academic_stage" in str(missing):
        reply_parts.append("Could you tell me your current academic stage? (e.g. After O/L, After A/L, or an Undergraduate)")
    elif "core_skills" in str(missing):
        reply_parts.append("What are some of your favorite skills or technical strengths? (e.g. Python, Math, Problem Solving)")
    elif "hobbies_interests" in str(missing):
        reply_parts.append("What hobbies, passion projects, or areas of technology excite you the most? (e.g. Robotics, Gaming, AI)")
    elif "career_ambitions" in str(missing):
        reply_parts.append("What is your dream career role or industry goal? (e.g. AI Engineer, Full-Stack Developer, Data Scientist)")

    fallback_reply = " ".join(reply_parts) if reply_parts else "Could you share a bit more about your background and interests?"
    return {"final_reply": fallback_reply}

# ----------------- Build LangGraph Workflow -----------------

def create_agent_1_graph():
    workflow = StateGraph(Agent1State)
    
    workflow.add_node("extract_slots", extraction_node)
    workflow.add_node("evaluate_progress", evaluation_node)
    workflow.add_node("generate_response", generation_node)

    workflow.set_entry_point("extract_slots")
    workflow.add_edge("extract_slots", "evaluate_progress")
    workflow.add_edge("evaluate_progress", "generate_response")
    workflow.add_edge("generate_response", END)

    return workflow.compile()

agent_1_app = create_agent_1_graph()

def run_agent_1_turn(
    user_message: str,
    history: List[Dict[str, str]],
    current_slots: ExtractedProfileSlots
) -> Agent1ChatResponse:
    """Executes a single conversational turn through the compiled LangGraph agent."""
    messages = []
    for h in history:
        if h.get("role") == "user":
            messages.append(HumanMessage(content=h.get("content", "")))
        elif h.get("role") == "assistant":
            messages.append(AIMessage(content=h.get("content", "")))

    initial_state: Agent1State = {
        "messages": messages,
        "current_message": user_message,
        "extracted_slots": current_slots or ExtractedProfileSlots(),
        "missing_slots": [],
        "is_complete": False,
        "final_reply": None,
    }

    final_state = agent_1_app.invoke(initial_state)

    return Agent1ChatResponse(
        reply_message=final_state["final_reply"] or "Thank you! Tell me more about your interests.",
        extracted_slots=final_state["extracted_slots"],
        missing_slots=final_state["missing_slots"],
        is_complete=final_state["is_complete"],
    )
