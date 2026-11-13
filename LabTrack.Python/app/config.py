import os
from pydantic_settings import BaseSettings
from pydantic import Field


class Settings(BaseSettings):
    """
    Application configuration settings loaded from environment variables
    with default values for development and containerized execution.
    """

    # Service Configuration
    APP_NAME: str = "LabTrack.Python"
    ENVIRONMENT: str = Field("development", validation_alias="ENVIRONMENT")
    DEBUG: bool = Field(True, validation_alias="DEBUG")
    PORT: int = Field(8000, validation_alias="PORT")

    # Security & Authentication
    JWT_SECRET_KEY: str = Field(
        "LabTrack_Secret_Key_For_Development_Only_Change_In_Production_2026",
        validation_alias="JWT_SECRET_KEY"
    )
    INTERNAL_SERVICE_SECRET: str = Field(
        "LabTrack_Internal_Service_Secret_Key_Change_In_Production_2026",
        validation_alias="INTERNAL_SERVICE_SECRET"
    )

    # External C# Backend Gateway
    CSHARP_API_BASE_URL: str = Field(
        "http://localhost:5000",
        validation_alias="CSHARP_API_BASE_URL"
    )

    # LLM Provider Configuration
    LLM_API_KEY: str = Field("your-llm-api-key-here", validation_alias="LLM_API_KEY")
    LLM_BASE_URL: str = Field("https://api.openai.com/v1", validation_alias="LLM_BASE_URL")
    LLM_DEFAULT_MODEL: str = Field("gpt-4o-mini", validation_alias="LLM_DEFAULT_MODEL")

    # Database Configuration
    DB_HOST: str = Field("localhost", validation_alias="DB_HOST")
    DB_PORT: int = Field(5432, validation_alias="DB_PORT")
    DB_NAME: str = Field("labtrack_db", validation_alias="DB_NAME")
    DB_USER: str = Field("postgres", validation_alias="DB_USER")
    DB_PASSWORD: str = Field("sven292005", validation_alias="DB_PASSWORD")

    @property
    def database_url(self) -> str:
        """Constructs the asynchronous PostgreSQL connection string for AsyncPG."""
        return f"postgresql+asyncpg://{self.DB_USER}:{self.DB_PASSWORD}@{self.DB_HOST}:{self.DB_PORT}/{self.DB_NAME}"

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = True


settings = Settings()