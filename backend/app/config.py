"""Configuracion del backend.

Los valores se leen del `.env` de la raiz del repositorio (nunca versionado).
En produccion se inyectan por variables de entorno del contenedor.
"""

from __future__ import annotations

from functools import lru_cache
from pathlib import Path

from pydantic import Field, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

REPO_ROOT = Path(__file__).resolve().parents[2]
ENV_FILE = REPO_ROOT / ".env"


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=ENV_FILE,
        env_file_encoding="utf-8",
        extra="ignore",
        case_sensitive=False,
    )

    # Entorno
    env: str = "dev"

    # Supabase (el backend es el unico que habla con el)
    supabase_url: str
    supabase_project_ref: str = ""
    supabase_publishable_key: str
    supabase_secret_key: str
    supabase_db_url: str
    supabase_db_url_session: str = ""

    # Secretos propios
    backend_jwt_secret: str = Field(min_length=32)
    backend_jwt_ttl_minutes: int = Field(default=30, ge=5, le=120)
    pin_pepper: str = Field(min_length=16)
    session_master_key: str = Field(min_length=16)

    # Reglas de juego y de seguridad
    session_ttl_minutes: int = Field(default=15, ge=1, le=60)
    max_points_per_second: int = Field(default=12, ge=1, le=100)
    max_score: int = 100_000
    login_max_attempts: int = Field(default=5, ge=3, le=20)
    login_lock_seconds: int = Field(default=60, ge=10, le=3600)

    # Red
    backend_port: int = 8000
    cors_allowed_origins: str = "https://juego.local"

    # Observabilidad
    log_level: str = "INFO"

    # Separacion de entornos (A.8.31). Ref del proyecto DEV: un token emitido
    # alli no puede usarse contra PROD ni al reves.
    dev_project_refs: str = ""

    @field_validator("env")
    @classmethod
    def _valid_env(cls, value: str) -> str:
        normalized = value.strip().lower()
        if normalized not in {"dev", "prod"}:
            raise ValueError("ENV debe ser 'dev' o 'prod'")
        return normalized

    @field_validator("supabase_url")
    @classmethod
    def _valid_url(cls, value: str) -> str:
        normalized = value.strip().rstrip("/")
        if not normalized.startswith("https://"):
            raise ValueError("SUPABASE_URL debe usar HTTPS")
        return normalized

    @property
    def cors_origins(self) -> list[str]:
        return [item.strip() for item in self.cors_allowed_origins.split(",") if item.strip()]

    @property
    def dev_refs(self) -> set[str]:
        return {item.strip() for item in self.dev_project_refs.split(",") if item.strip()}

    @property
    def is_prod(self) -> bool:
        return self.env == "prod"

    def assert_environment(self) -> None:
        """Impide arrancar PROD apuntando al proyecto DEV.

        Es la parte ejecutable de A.8.31: no basta con documentar la separacion,
        el proceso se niega a levantar si las claves no corresponden al entorno.
        """
        if self.is_prod and self.supabase_project_ref and self.supabase_project_ref in self.dev_refs:
            raise RuntimeError(
                "ENV=prod apunta a un proyecto DEV: revisar SUPABASE_PROJECT_REF y las claves"
            )


@lru_cache
def get_settings() -> Settings:
    settings = Settings()  # type: ignore[call-arg]
    settings.assert_environment()
    return settings
