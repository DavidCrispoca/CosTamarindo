"""Aplicacion FastAPI: composicion, middleware y manejo de errores."""

from __future__ import annotations

import asyncio
import logging
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from sqlalchemy.exc import SQLAlchemyError

from .config import Settings, get_settings
from .db import Database
from .errors import DomainError
from .logging_setup import audit, configure_logging, new_request_id, request_id_var, user_id_var
from .providers.supabase_auth import SupabaseAuth
from .repositories import Repository, SqlRepository
from .routers import auth, dialog, health, leaderboard, score, session

logger = logging.getLogger("costamarindo.http")


def create_app(
    *,
    settings: Settings | None = None,
    repository: Repository | None = None,
    auth_provider: SupabaseAuth | None = None,
) -> FastAPI:
    settings = settings or get_settings()
    configure_logging(settings.log_level)

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        database: Database | None = None
        if repository is None:
            database = Database(settings)
            app.state.repository = SqlRepository(database.session_factory)
        else:
            app.state.repository = repository
        app.state.auth_provider = auth_provider or SupabaseAuth(
            base_url=settings.supabase_url,
            publishable_key=settings.supabase_publishable_key,
            secret_key=settings.supabase_secret_key,
        )

        # Calentamiento con reintentos: las primeras conexiones desde un
        # contenedor recien creado pueden fallar mientras se asienta el DNS
        # interno o el pooler. Se registra, no se aborta: /api/health lo reporta.
        if database is not None:
            for attempt in range(1, 6):
                try:
                    await app.state.repository.ping()
                    audit("db_ready", attempt=attempt)
                    break
                except Exception:  # noqa: BLE001 - el motivo real va al log
                    logger.warning(
                        "db_warmup_retry",
                        extra={"event": "db_warmup_retry", "fields": {"attempt": attempt}},
                    )
                    if attempt == 5:
                        audit("db_warmup_failed", attempts=attempt)
                    else:
                        await asyncio.sleep(2 * attempt)

        audit("app_start", env=settings.env)
        try:
            yield
        finally:
            if database is not None:
                await database.dispose()

    app = FastAPI(
        title="CosTamarindo API",
        version="1.0.0",
        lifespan=lifespan,
        docs_url=None if settings.is_prod else "/api/docs",
        redoc_url=None,
        openapi_url=None if settings.is_prod else "/api/openapi.json",
    )

    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.cors_origins,
        allow_credentials=False,
        allow_methods=["GET", "POST"],
        allow_headers=["Authorization", "Content-Type"],
        max_age=600,
    )

    @app.middleware("http")
    async def request_context(request: Request, call_next):
        request_id = new_request_id()
        token = request_id_var.set(request_id)
        user_id_var.set("-")
        try:
            response = await call_next(request)
        finally:
            request_id_var.reset(token)
        response.headers["X-Request-ID"] = request_id
        return response

    @app.exception_handler(DomainError)
    async def domain_error_handler(request: Request, exc: DomainError) -> JSONResponse:
        headers = {}
        if exc.status_code == 429:
            headers["Retry-After"] = "60"
        if exc.status_code == 423:
            headers["Retry-After"] = str(settings.login_lock_seconds)
        if exc.status_code == 401:
            headers["WWW-Authenticate"] = "Bearer"
        return JSONResponse(
            status_code=exc.status_code,
            content={"code": exc.code, "message": exc.message},
            headers=headers,
        )

    @app.exception_handler(RequestValidationError)
    async def validation_error_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        # No se devuelve el detalle de Pydantic: puede reflejar rutas o tipos internos.
        return JSONResponse(
            status_code=422,
            content={"code": "invalid_request", "message": "Datos de entrada invalidos"},
        )

    @app.exception_handler(SQLAlchemyError)
    async def database_error_handler(request: Request, exc: SQLAlchemyError) -> JSONResponse:
        # Un fallo transitorio de base no es un 500 opaco: es 503 y queda auditado.
        detail = getattr(exc, "orig", None) or exc
        logger.warning(
            "database_error",
            extra={
                "event": "database_error",
                "fields": {
                    "path": request.url.path,
                    "error": type(detail).__name__,
                    "detail": str(detail)[:200],
                },
            },
        )
        return JSONResponse(
            status_code=503,
            content={"code": "database_unavailable", "message": "Servicio no disponible"},
        )

    @app.exception_handler(Exception)
    async def unhandled_error_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.exception("unhandled_error", extra={"event": "unhandled_error", "fields": {}})
        return JSONResponse(
            status_code=500,
            content={"code": "internal_error", "message": "Error interno"},
        )

    app.include_router(health.router)
    app.include_router(auth.router)
    app.include_router(session.router)
    app.include_router(score.router)
    app.include_router(leaderboard.router)
    app.include_router(dialog.router)

    return app


app = create_app()
