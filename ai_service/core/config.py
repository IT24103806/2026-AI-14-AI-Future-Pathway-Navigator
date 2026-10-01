import os
from pydantic_settings import BaseSettings, SettingsConfigDict
from dotenv import load_dotenv

load_dotenv()

class Settings(BaseSettings):
    model_config = SettingsConfigDict(case_sensitive=True)
    PROJECT_NAME: str = "PathwayNavigator AI Microservice"
    VERSION: str = "1.0.0"
    API_V1_STR: str = "/api/v1"
    
    # LLM Provider Configuration
    # Supported: "google" or "openai"
    LLM_PROVIDER: str = os.getenv("LLM_PROVIDER", "google")
    
    # API Keys
    GOOGLE_API_KEY: str = os.getenv("GOOGLE_API_KEY", "")
    OPENAI_API_KEY: str = os.getenv("OPENAI_API_KEY", "")
    
    # Model Configurations
    GOOGLE_MODEL: str = os.getenv("GOOGLE_MODEL", "gemini-2.5-flash")
    OPENAI_MODEL: str = os.getenv("OPENAI_MODEL", "gpt-4o-mini")
    
    # CORS
    BACKEND_CORS_ORIGINS: list[str] = [
        "http://localhost:5081",
        "https://localhost:7001",
        "http://localhost:5173",
        "http://localhost:3000"
    ]

settings = Settings()
