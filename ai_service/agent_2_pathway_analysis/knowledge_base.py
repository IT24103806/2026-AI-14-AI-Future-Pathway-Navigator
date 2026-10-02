"""
Curated career-pathway knowledge base for Agent 2 (Career Discovery Agent)
and Agent 3 (Pathway Planner Agent).

This is the *allow-listed* data source the agents are permitted to read from.
It is intentionally static/local (no arbitrary web scraping) so results are
explainable, reproducible during evaluation, and safe from prompt injection
via retrieved content.

Each pathway includes:
- Core skill/interest/ambition matching metadata
- Ordered learning courses (Foundation -> Core Skills -> Specialization)
- Career deep-dive (day-in-the-life, Sri Lankan & remote salary bands, industry tools)
- Concrete starter portfolio project blueprints
- Industry-recognized certifications
- Verified Sri Lankan higher-education routes (State Universities, SLIIT/Private Degrees,
  OUSL/BIT low-budget routes, Foundation certificates, and TVEC NVQ Level 4/5/6 diplomas)
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
    day_in_the_life: str
    salary_range_lkr: str
    industry_tools: List[str]
    portfolio_projects: List[str]
    recommended_certifications: List[str]
    sri_lankan_education_routes: List[str]


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
        "day_in_the_life": (
            "Collaborates with data and product teams to prepare datasets, train and evaluate deep learning or "
            "LLM pipelines, optimize inference latency, and deploy monitored AI microservices into production."
        ),
        "salary_range_lkr": "LKR 180,000 – 450,000/mo (Entry–Mid SL) | USD 2,000 – 4,500/mo (Global Remote)",
        "industry_tools": ["Python", "PyTorch", "Scikit-learn", "FastAPI", "Docker", "Hugging Face", "MLflow", "Git"],
        "portfolio_projects": [
            "End-to-End Sinhala/English Document Q&A Assistant using RAG and FastAPI",
            "Customer Churn Prediction Pipeline with Automated Feature Engineering and Model Monitoring",
            "Computer Vision Defect Classifier Deployed as a Containerized REST Microservice",
        ],
        "recommended_certifications": [
            "DeepLearning.AI Machine Learning Specialization",
            "AWS Certified Machine Learning – Specialty / Associate",
            "Google Cloud Professional Machine Learning Engineer",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Artificial Intelligence / Data Science",
            "State University BSc (Hons) in Computer Science & Engineering (UoM, UCSC, UoP) via Physical Science A/L Z-score",
            "Low-Budget / Flexible Route: UCSC BIT or Open University (OUSL) BSc in Software Engineering + Open ML Specializations",
            "Bridge / After O/L Route: SLIIT Computing Foundation Certificate or TVEC NVQ Level 5/6 ICT Diploma -> Degree Lateral Entry",
        ],
    },
    {
        "pathway_name": "Full-Stack Software Engineer",
        "description": "Designs and builds end-to-end web and mobile applications.",
        "required_skills": ["javascript", "react", "c#", "sql", "git", "problem solving"],
        "related_interests": ["web development", "coding", "gaming", "open source"],
        "matching_career_terms": ["software engineer", "full stack developer", "web developer", "software architect"],
        "recommended_courses": ["ASP.NET Core Web API", "React & Modern Frontend", "System Design Basics"],
        "search_term": "full stack software engineer",
        "day_in_the_life": (
            "Designs REST/GraphQL APIs and relational database schemas, builds responsive React/TypeScript interfaces, "
            "participates in agile sprint planning and code reviews, and ships automated CI/CD releases."
        ),
        "salary_range_lkr": "LKR 150,000 – 380,000/mo (Entry–Mid SL) | USD 1,800 – 4,000/mo (Global Remote)",
        "industry_tools": ["C# / .NET 8", "React", "TypeScript", "PostgreSQL", "Docker", "Git / GitHub Actions", "Postman"],
        "portfolio_projects": [
            "Role-Based SaaS Booking & Billing Platform with ASP.NET Core Web API, PostgreSQL, and React",
            "Real-Time Collaborative Task Board with WebSockets, JWT Refresh Tokens, and Automated CI Tests",
            "E-Commerce Order & Inventory Microservice with Redis Caching and Stripe/PayHere Sandbox Integration",
        ],
        "recommended_certifications": [
            "Microsoft Certified: Azure Developer Associate (AZ-204)",
            "Meta Front-End / Back-End Developer Professional Certificate",
            "AWS Certified Developer – Associate",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Software Engineering",
            "State University BSc (Hons) in Computer Science / Information Systems (UCSC, UoM, UoK, USJ)",
            "Low-Budget / Government Loan Route: OUSL BSc (Hons) in Software Engineering, UCSC/UoM BIT, or IFSLS Interest-Free Student Loan Degree",
            "Bridge / Vocational Route: TVEC NVQ Level 5 National Diploma in ICT (SLIATE HNDIT / VTA) -> BSc Top-Up",
        ],
    },
    {
        "pathway_name": "Data Scientist / Data Analyst",
        "description": "Extracts insights and builds predictive models from data.",
        "required_skills": ["python", "sql", "data analysis", "statistics", "machine learning"],
        "related_interests": ["data analysis", "ai", "mathematics", "research"],
        "matching_career_terms": ["data scientist", "data analyst", "business intelligence"],
        "recommended_courses": ["Applied Statistics", "Data Visualization", "SQL for Analytics"],
        "search_term": "data scientist",
        "day_in_the_life": (
            "Queries large operational warehouses with SQL, cleans and explores trends in Python/Pandas, builds "
            "forecasting models and interactive Power BI/Tableau dashboards, and presents actionable metrics to stakeholders."
        ),
        "salary_range_lkr": "LKR 140,000 – 350,000/mo (Entry–Mid SL) | USD 1,600 – 3,800/mo (Global Remote)",
        "industry_tools": ["Python", "Pandas", "SQL", "Power BI", "Tableau", "Jupyter", "Scikit-learn", "dbt"],
        "portfolio_projects": [
            "Sri Lanka Economic & Tourism Recovery Interactive Analytics Dashboard (SQL + Power BI / Plotly)",
            "Retail Demand Forecasting & Inventory Optimization Model with Time-Series Analysis",
            "Automated ETL & Cohort Retention Pipeline Analyzing E-Commerce User Behavior",
        ],
        "recommended_certifications": [
            "Google Advanced Data Analytics Professional Certificate",
            "Microsoft Certified: Power BI Data Analyst Associate (PL-300)",
            "IBM Data Science Professional Certificate",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Data Science",
            "State University BSc in Statistics / Operations Research / Data Science (UoC, USJ, UoM, UoP)",
            "Low-Budget Route: UCSC BIT / OUSL BSc combined with SLASSCOM / Coursera Data Analytics Specializations",
            "Commerce / Arts Bridge Route: Accredited Computing Foundation + Business Analytics Diploma (NIBM / IIT / SLIIT)",
        ],
    },
    {
        "pathway_name": "Cybersecurity Analyst",
        "description": "Protects systems and networks from security threats.",
        "required_skills": ["networking", "linux", "problem solving", "python", "security"],
        "related_interests": ["cyber security", "ethical hacking", "gaming", "open source"],
        "matching_career_terms": ["cybersecurity analyst", "security engineer", "penetration tester"],
        "recommended_courses": ["Network Security Fundamentals", "Ethical Hacking Basics", "Cloud Security"],
        "search_term": "cyber security analyst",
        "day_in_the_life": (
            "Monitors SIEM alerts in a Security Operations Center (SOC), performs vulnerability scans and web application "
            "penetration tests, hardens Linux/cloud infrastructure, and leads incident response playbooks."
        ),
        "salary_range_lkr": "LKR 160,000 – 400,000/mo (Entry–Mid SL) | USD 1,900 – 4,200/mo (Global Remote)",
        "industry_tools": ["Linux", "Wireshark", "Nmap", "Burp Suite", "Splunk / Wazuh SIEM", "OWASP ZAP", "Python"],
        "portfolio_projects": [
            "Home SOC Lab with Wazuh SIEM, Automated Intrusion Detection, and Incident Response Playbook",
            "OWASP Top 10 Web Application Vulnerability Audit & Remediation Report on a Demo API",
            "Python Automated Network & Cloud Security Posture Scanner with CVE Reporting",
        ],
        "recommended_certifications": [
            "CompTIA Security+ (SY0-701)",
            "ISC2 Certified in Cybersecurity (CC) – Free Entry-Level Exam",
            "eJPT (Junior Penetration Tester) / CEH (Certified Ethical Hacker)",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Cyber Security",
            "Cicra Campus / IIT / NSBM Information Security & Networking Degree Programmes",
            "Low-Budget / Public Route: SLIATE HNDIT or OUSL BSc + ISC2 CC & TryHackMe/HackTheBox Verifiable Labs",
            "Bridge / NVQ Route: TVEC NVQ Level 5 Diploma in Network & System Administration -> Cyber Security Degree",
        ],
    },
    {
        "pathway_name": "UI/UX Designer",
        "description": "Designs usable, engaging digital product experiences.",
        "required_skills": ["figma", "ui/ux", "graphic design", "communication", "html", "css"],
        "related_interests": ["drawing", "photography", "design", "ui/ux"],
        "matching_career_terms": ["ui/ux designer", "product designer", "graphic designer"],
        "recommended_courses": ["UX Research Fundamentals", "Figma for Product Design", "Design Systems"],
        "search_term": "ui ux designer",
        "day_in_the_life": (
            "Conducts user interviews and usability tests, maps user journeys and wireframes, builds accessible "
            "interactive prototypes and component design systems in Figma, and partners with frontend engineers."
        ),
        "salary_range_lkr": "LKR 120,000 – 320,000/mo (Entry–Mid SL) | USD 1,500 – 3,500/mo (Global Remote)",
        "industry_tools": ["Figma", "FigJam", "Maze / UsabilityHub", "Adobe Creative Cloud", "HTML5 / CSS3", "Storybook"],
        "portfolio_projects": [
            "Complete UX Case Study: Redesigning a Sri Lankan Public Transport / Railway Ticketing Mobile App",
            "Multi-Brand Accessible Design System in Figma with Token Variables, Dark Mode, and WCAG 2.1 AA Compliance",
            "End-to-End FinTech Onboarding Flow with Interactive Prototype and Moderated Usability Test Findings",
        ],
        "recommended_certifications": [
            "Google UX Design Professional Certificate",
            "Interaction Design Foundation (IxDF) UX Bootcamp / Certificates",
            "W3C Web Accessibility (WAI) Foundations Certificate",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Interactive Media",
            "University of Moratuwa Bachelor of Design (BDes) / UCSC Information Systems",
            "Academy of Design (AOD) / AMDT / Raffles Design or NIBM Digital Design Diplomas",
            "Low-Budget / Non-STEM Route: TVEC NVQ Level 5 Multimedia & Web Design + Strong 3-Case-Study Behance/Figma Portfolio",
        ],
    },
    {
        "pathway_name": "Cloud / DevOps Engineer",
        "description": "Builds and maintains scalable cloud infrastructure and deployment pipelines.",
        "required_skills": ["cloud", "linux", "git", "problem solving", "networking"],
        "related_interests": ["cloud", "iot", "open source", "automation"],
        "matching_career_terms": ["cloud engineer", "devops engineer", "site reliability engineer"],
        "recommended_courses": ["AWS/Azure Fundamentals", "Docker & Kubernetes", "CI/CD Pipelines"],
        "search_term": "cloud devops engineer",
        "day_in_the_life": (
            "Automates cloud infrastructure using Terraform, containerizes microservices with Docker and Kubernetes, "
            "maintains zero-downtime GitHub Actions/GitLab CI pipelines, and monitors system reliability with Prometheus/Grafana."
        ),
        "salary_range_lkr": "LKR 170,000 – 430,000/mo (Entry–Mid SL) | USD 2,000 – 4,500/mo (Global Remote)",
        "industry_tools": ["AWS / Azure", "Docker", "Kubernetes", "Terraform", "Linux", "GitHub Actions", "Prometheus & Grafana"],
        "portfolio_projects": [
            "Multi-Container Microservice Deployed on AWS/Azure Free Tier using Terraform and GitHub Actions CI/CD",
            "Kubernetes Cluster Auto-Scaling & Observability Stack with Prometheus, Grafana, and Alerting",
            "Automated DevSecOps Pipeline with Container Image Vulnerability Scanning (Trivy) and Rollback Strategy",
        ],
        "recommended_certifications": [
            "AWS Certified Solutions Architect – Associate (SAA-C03)",
            "Microsoft Certified: Azure Administrator Associate (AZ-104)",
            "HashiCorp Certified: Terraform Associate / CKA (Certified Kubernetes Administrator)",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Computer Systems & Network Engineering",
            "State University BSc in Computer Science / Engineering (UoM, UCSC, UoK) + Cloud Certifications",
            "Low-Budget Route: SLIATE HNDIT / UCSC BIT + AWS Educate / Microsoft Learn Student Ambassador Tracks",
            "Vocational / NVQ Route: TVEC NVQ Level 5/6 Systems & Network Engineering Diploma -> BSc Lateral Entry",
        ],
    },
    {
        "pathway_name": "Mobile App Developer",
        "description": "Builds native and cross-platform mobile applications.",
        "required_skills": ["dart", "flutter", "java", "problem solving", "ui/ux"],
        "related_interests": ["coding", "gaming", "app development", "design"],
        "matching_career_terms": ["mobile developer", "app developer", "flutter developer"],
        "recommended_courses": ["Flutter & Dart Fundamentals", "Mobile UI Design", "REST API Integration"],
        "search_term": "mobile app developer",
        "day_in_the_life": (
            "Develops smooth cross-platform Android and iOS apps in Flutter/Dart or Kotlin, integrates secure REST APIs "
            "and offline storage, writes widget and unit tests, and manages Google Play / App Store releases."
        ),
        "salary_range_lkr": "LKR 140,000 – 360,000/mo (Entry–Mid SL) | USD 1,700 – 3,800/mo (Global Remote)",
        "industry_tools": ["Flutter", "Dart", "Kotlin / Android Studio", "Firebase", "REST APIs", "Git", "Codemagic / Fastlane"],
        "portfolio_projects": [
            "Offline-First Personal Finance & Expense Tracker App in Flutter with Local Encryption and Cloud Sync",
            "Campus Delivery / Ride-Sharing Mobile App with Live Maps, Push Notifications, and JWT Authentication",
            "Accessible Telehealth Appointment & Prescription Tracker with Full Widget Test Coverage",
        ],
        "recommended_certifications": [
            "Google Associate Android Developer / Flutter Complete Development Bootcamp",
            "Meta React Native / Cross-Platform Mobile Developer Certificate",
            "AWS Certified Cloud Practitioner (for Mobile Backend Integration)",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Software Engineering",
            "IIT / NSBM / NIBM BSc (Hons) Software Engineering or Mobile Application Development Tracks",
            "Low-Budget Route: OUSL BSc in Software Engineering or UCSC BIT + Published Google Play Store Portfolio",
            "Bridge / After O/L Route: TVEC NVQ Level 4/5 Software Developer Certificate -> Diploma -> BSc Degree",
        ],
    },
    {
        "pathway_name": "Game Developer",
        "description": "Designs and programs interactive games and simulations.",
        "required_skills": ["c++", "c#", "problem solving", "mathematics"],
        "related_interests": ["gaming", "drawing", "music", "3d modelling"],
        "matching_career_terms": ["game developer", "game designer", "game programmer"],
        "recommended_courses": ["Unity Game Development", "Game Design Principles", "3D Graphics Basics"],
        "search_term": "game developer",
        "day_in_the_life": (
            "Programs gameplay mechanics, physics, AI pathfinding, and shaders in Unity (C#) or Unreal Engine (C++), "
            "collaborates with 3D artists and sound designers, and profiles frame-rate performance across devices."
        ),
        "salary_range_lkr": "LKR 130,000 – 340,000/mo (Entry–Mid SL) | USD 1,600 – 3,800/mo (Global Remote / Indie)",
        "industry_tools": ["Unity (C#)", "Unreal Engine (C++)", "Godot", "Blender", "Git LFS", "C# / C++", "Shader Graph"],
        "portfolio_projects": [
            "Playable 3D Action-Puzzle Prototype in Unity with Custom Physics, State-Machine AI, and WebGL Build on itch.io",
            "2D Multiplayer Co-op Platformer with Netcode, Leaderboard API, and Custom Particle Effects",
            "AR/VR Interactive Heritage or Training Simulation Built with Unity XR Toolkit",
        ],
        "recommended_certifications": [
            "Unity Certified Associate / Unity Certified Programmer",
            "Epic Games Unreal Engine Game Development Specialization",
            "C++ Institute Certified Associate Programmer (CPA)",
        ],
        "sri_lankan_education_routes": [
            "SLIIT BSc (Hons) in Information Technology Specializing in Interactive Media / Software Engineering",
            "AMDT / AOD / IIT Game Development & Interactive Digital Media Degree Routes",
            "Low-Budget Route: UCSC BIT / OUSL BSc + Global Game Jam Participation & Published itch.io/Steam Prototypes",
            "Bridge Route: Computing Foundation Certificate + TVEC NVQ Level 5 Creative Multimedia & Game Design Diploma",
        ],
    },
]
