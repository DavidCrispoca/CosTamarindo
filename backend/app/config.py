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
    # Pooler de sesion (5432): el indicado para un backend con pool de
    # conexiones persistentes. Los scripts de backup (pg_dump) usan la misma URL.
    supabase_db_url: str

    # Secretos propios
    backend_jwt_secret: str = Field(min_length=32)
    backend_jwt_ttl_minutes: int = Field(default=30, ge=5, le=120)
    pin_pepper: str = Field(min_length=16)
    session_master_key: str = Field(min_length=16)

    # Reglas de juego y de seguridad
    session_ttl_minutes: int = Field(default=15, ge=1, le=60)
    # Recalibrado al balance real: los ítems dan 10-25 pts y el spawner suelta
    # ~1.5-2.6 ítems/s, así que un buen jugador supera con holgura 12 pts/s.
    # 40 pts/s (4800 pts en 120 s) queda por encima del máximo humano
    # (~20-25 pts/s) sin anular la defensa antifraude: las reglas de captura
    # (score <= catches*25) y de ritmo (catches <= seconds*3) siguen activas.
    max_points_per_second: int = Field(default=40, ge=1, le=100)
    max_score: int = 100_000
    login_max_attempts: int = Field(default=5, ge=3, le=20)
    login_lock_seconds: int = Field(default=60, ge=10, le=3600)

    # Red
    backend_port: int = 8000
    cors_allowed_origins: str = "https://juego.local"

    # IA (Sg-AI). El backend es el único que habla con Ollama; el puerto 11434
    # nunca se publica. Si la IA falla, el diálogo usa el banco de respaldo.
    ollama_url: str = "http://sg-ai:11434"
    ollama_model: str = "qwen2.5:3b"
    ollama_timeout_seconds: int = Field(default=20, ge=1, le=120)
    ollama_connect_timeout_seconds: float = Field(default=0.75, gt=0, le=5)
    ollama_max_concurrency: int = Field(default=1, ge=1, le=4)
    ollama_num_predict: int = Field(default=120, ge=16, le=512)
    ollama_temperature: float = Field(default=0.8, ge=0.0, le=2.0)

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
