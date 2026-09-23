import pytest
from agent_4_reality_check.agent import evaluate_reality_check
from agent_4_reality_check.schemas import RealityCheckRequest

def test_high_risk_stream_mismatch_arts_to_ai():
    """Arts stream ශිෂ්‍යයෙක් AI Engineering තේරූ විට high-risk flag විය යුතුය"""
    request = RealityCheckRequest(
        student_id="student-001",
        target_career="AI Engineer",
        al_stream="Arts",
        al_results="A, B, C",
        budget_level="Medium",
        current_skills=["English", "Communication"]
    )
    
    response = evaluate_reality_check(request)
    
    assert response.is_high_risk is True
    assert response.counsellor_review_required is True
    assert "Stream mismatch" in response.risk_reason
    assert response.feasibility_score < 80

def test_low_budget_international_risk():
    """Low budget සහිතව International/Abroad තේරූ විට Financial risk එකක් flag විය යුතුය"""
    request = RealityCheckRequest(
        student_id="student-002",
        target_career="Software Engineer (Abroad Degree)",
        al_stream="Physical Science",
        al_results="A, B, B",
        budget_level="Low",
        current_skills=["Python", "OOP", "Git"]
    )
    
    response = evaluate_reality_check(request)
    
    assert response.is_high_risk is True
    assert "Financial risk" in response.risk_reason
    assert response.counsellor_review_required is True

def test_feasible_pathway_direct_approval():
    """සුදුසුකම් සපිරූ ශිෂ්‍යයෙකුට Counsellor review අවශ්‍ය නොවිය යුතුය"""
    request = RealityCheckRequest(
        student_id="student-003",
        target_career="Software Engineer",
        al_stream="Physical Science",
        al_results="A, A, B",
        budget_level="Medium",
        current_skills=["Python", "OOP", "Data Structures", "Git", "Databases"]
    )
    
    response = evaluate_reality_check(request)
    
    assert response.is_high_risk is False
    assert response.counsellor_review_required is False
    assert response.feasibility_score >= 80
    assert response.is_feasible is True