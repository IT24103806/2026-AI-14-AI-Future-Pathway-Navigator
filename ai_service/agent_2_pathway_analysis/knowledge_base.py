"""
Curated career-pathway knowledge base for Agent 2 (Career Discovery Agent).

This is the *allow-listed* data source the agent is permitted to read from.
It is intentionally static/local (no arbitrary web scraping) so results are
explainable, reproducible during evaluation, and safe from prompt injection
via retrieved content.

Extend this list with more pathways as needed — each entry is used for
deterministic skill/interest matching, and 'search_term' is the query sent
to the external market-data tool.
"""

from typing import List, TypedDict


class PathwayDefinition(TypedDict):
    pathway_name: str
    description: str
    required_skills: List[str]
    related_interests: List[str]
    matching_career_terms: List[str]  # matched against free-text career_ambitions
    recommended_courses: List[str]
    search_term: str  # job title used for market-data lookups


# Ordered learning phases. Each pathway's `recommended_courses` list is written
# in the order a student should take them, so we zip it 1:1 with these labels
# to build the roadmap deterministically (no LLM involved -> no hallucinated
# course ordering).
ROADMAP_PHASE_LABELS = ["Foundation", "Core Skills", "Specialization"]


CAREER_PATHWAYS: List[PathwayDefinition] = [
    {
        "pathway_name": "AI / Machine Learning Engineer",
        "description": "Builds and deploys machine learning models and intelligent systems.",
        "required_skills": ["python", "machine learning", "deep learning", "sql", "problem solving"],
        "related_interests": ["ai", "artificial intelligence", "robotics", "data analysis", "open source"],
        "matching_career_terms": ["ai engineer", "machine learning engineer", "ml engineer", "data scientist"],
        "recommended_courses": ["Machine Learning Specialization", "Deep Learning with PyTorch", "MLOps Fundamentals"],
        "search_term": "machine learning engineer",
    },
    {
        "pathway_name": "Full-Stack Software Engineer",
        "description": "Designs and builds end-to-end web and mobile applications.",
        "required_skills": ["javascript", "react", "c#", "sql", "git", "problem solving"],
        "related_interests": ["web development", "coding", "gaming", "open source"],
        "matching_career_terms": ["software engineer", "full stack developer", "web developer", "software architect"],
        "recommended_courses": ["ASP.NET Core Web API", "React & Modern Frontend", "System Design Basics"],
        "search_term": "full stack software engineer",
    },
    {
        "pathway_name": "Data Scientist / Data Analyst",
        "description": "Extracts insights and builds predictive models from data.",
        "required_skills": ["python", "sql", "data analysis", "statistics", "machine learning"],
        "related_interests": ["data analysis", "ai", "mathematics", "research"],
        "matching_career_terms": ["data scientist", "data analyst", "business intelligence"],
        "recommended_courses": ["Applied Statistics", "Data Visualization", "SQL for Analytics"],
        "search_term": "data scientist",
    },
    {
        "pathway_name": "Cybersecurity Analyst",
        "description": "Protects systems and networks from security threats.",
        "required_skills": ["networking", "linux", "problem solving", "python", "security"],
        "related_interests": ["cyber security", "ethical hacking", "gaming", "open source"],
        "matching_career_terms": ["cybersecurity analyst", "security engineer", "penetration tester"],
        "recommended_courses": ["Network Security Fundamentals", "Ethical Hacking Basics", "Cloud Security"],
        "search_term": "cyber security analyst",
    },
    {
        "pathway_name": "UI/UX Designer",
        "description": "Designs usable, engaging digital product experiences.",
        "required_skills": ["figma", "ui/ux", "graphic design", "communication", "html", "css"],
        "related_interests": ["drawing", "photography", "design", "ui/ux"],
        "matching_career_terms": ["ui/ux designer", "product designer", "graphic designer"],
        "recommended_courses": ["UX Research Fundamentals", "Figma for Product Design", "Design Systems"],
        "search_term": "ui ux designer",
    },
    {
        "pathway_name": "Cloud / DevOps Engineer",
        "description": "Builds and maintains scalable cloud infrastructure and deployment pipelines.",
        "required_skills": ["cloud", "linux", "git", "problem solving", "networking"],
        "related_interests": ["cloud", "iot", "open source", "automation"],
        "matching_career_terms": ["cloud engineer", "devops engineer", "site reliability engineer"],
        "recommended_courses": ["AWS/Azure Fundamentals", "Docker & Kubernetes", "CI/CD Pipelines"],
        "search_term": "cloud devops engineer",
    },
    {
        "pathway_name": "Mobile App Developer",
        "description": "Builds native and cross-platform mobile applications.",
        "required_skills": ["dart", "flutter", "java", "problem solving", "ui/ux"],
        "related_interests": ["coding", "gaming", "app development", "design"],
        "matching_career_terms": ["mobile developer", "app developer", "flutter developer"],
        "recommended_courses": ["Flutter & Dart Fundamentals", "Mobile UI Design", "REST API Integration"],
        "search_term": "mobile app developer",
    },
    {
        "pathway_name": "Game Developer",
        "description": "Designs and programs interactive games and simulations.",
        "required_skills": ["c++", "c#", "problem solving", "mathematics"],
        "related_interests": ["gaming", "drawing", "music", "3d modelling"],
        "matching_career_terms": ["game developer", "game designer", "game programmer"],
        "recommended_courses": ["Unity Game Development", "Game Design Principles", "3D Graphics Basics"],
        "search_term": "game developer",
    },
]
