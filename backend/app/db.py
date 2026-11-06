"""Motor de base de datos.

Se conecta por el pooler de Supabase en modo transaccion (6543). Ese modo no
admite sentencias preparadas con nombre, por eso `statement_cache_size = 0`;
sin eso asyncpg falla de forma intermitente con "prepared statement already
exists".
"""

from __future__ import annotations

from collections.abc import AsyncIterator

from sqlalchemy.ext.asyncio import AsyncEngine, AsyncSession, async_sessionmaker, create_async_engine

from .config import Settings


def to_asyncpg_url(url: str) -> str:
    if url.startswith("postgresql+asyncpg://"):
        return url
    if url.startswith("postgresql://"):
        return "postgresql+asyncpg://" + url[len("postgresql://") :]
    if url.startswith("postgres://"):
        return "postgresql+asyncpg://" + url[len("postgres://") :]
    return url


def create_engine(settings: Settings) -> AsyncEngine:
    # El pooler de Supabase en modo transaccion no soporta sentencias preparadas
    # con nombre. Hay dos cachés que hay que desactivar: la de asyncpg
    # (statement_cache_size) y la del dialecto de SQLAlchemy
    # (prepared_statement_cache_size). Sin la segunda aparece
    # DuplicatePreparedStatementError de forma intermitente.
    connect_args: dict[str, object] = {
        "statement_cache_size": 0,
        "prepared_statement_cache_size": 0,
    }
    if not settings.supabase_db_url.startswith("postgresql://localhost"):
        connect_args["ssl"] = "require"
    return create_async_engine(
        to_asyncpg_url(settings.supabase_db_url),
        pool_pre_ping=True,
        pool_size=5,
        max_overflow=5,
        pool_recycle=1800,
        connect_args=connect_args,
    )


class Database:
    """Contenedor del engine y de la fabrica de sesiones."""

    def __init__(self, settings: Settings) -> None:
        self.engine = create_engine(settings)
        self.session_factory = async_sessionmaker(self.engine, expire_on_commit=False)

    async def session(self) -> AsyncIterator[AsyncSession]:
        async with self.session_factory() as session:
            yield session

    async def dispose(self) -> None:
        await self.engine.dispose()
