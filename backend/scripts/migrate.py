"""Aplica las migraciones SQL de backend/sql en orden.

Uso:
    python backend/scripts/migrate.py            # aplica todas las pendientes
    python backend/scripts/migrate.py --dry-run  # solo lista lo que haria
    python backend/scripts/migrate.py --status   # muestra el estado

Se conecta con SUPABASE_DB_URL (pooler en modo sesion), que es el unico
endpoint que admite sentencias de DDL en una transaccion larga. No usa la
Management API: cualquiera con las credenciales de la base puede reproducirlo.
"""

from __future__ import annotations

import argparse
import asyncio
import hashlib
import os
import sys
from pathlib import Path

import asyncpg

SQL_DIR = Path(__file__).resolve().parent.parent / "sql"
ROOT = SQL_DIR.parent.parent


def load_env(path: Path) -> None:
    """Carga el .env en os.environ sin sobrescribir lo ya definido."""
    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip())


def database_url() -> str:
    url = os.environ.get("SUPABASE_DB_URL")
    if not url:
        sys.exit("Falta SUPABASE_DB_URL o SUPABASE_DB_URL en el entorno o en .env")
    return url


async def ensure_migrations_table(conn: asyncpg.Connection) -> None:
    """El historial vive en el schema private, fuera de la superficie de datos.

    Se crea antes de leer el estado para que la primera ejecucion pueda registrar
    su propia migracion. La 003 lo replica por si alguien aplica los .sql a mano.
    """
    await conn.execute(
        """
        create schema if not exists private;
        revoke all on schema private from public;
        create table if not exists private.schema_migrations (
            version    text primary key,
            checksum   text not null,
            applied_at timestamptz not null default now()
        );
        alter table private.schema_migrations enable row level security;
        revoke all on private.schema_migrations from public, anon, authenticated;
        """
    )


def migration_files() -> list[Path]:
    return sorted(p for p in SQL_DIR.glob("*.sql") if p.is_file())


def checksum(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


async def run(dry_run: bool, status_only: bool) -> int:
    url = database_url()
    conn = await asyncpg.connect(url, ssl="require", statement_cache_size=0)
    try:
        await ensure_migrations_table(conn)
        applied = {
            row["version"]: row
            for row in await conn.fetch("select version, checksum, applied_at from private.schema_migrations")
        }

        pending: list[Path] = []
        for path in migration_files():
            version = path.name
            if version not in applied:
                pending.append(path)
                continue
            if applied[version]["checksum"] != checksum(path):
                print(f"AVISO: {version} cambio despues de aplicarse (checksum distinto)")

        if status_only:
            for path in migration_files():
                row = applied.get(path.name)
                estado = f"aplicada {row['applied_at']:%Y-%m-%d %H:%M}" if row else "pendiente"
                print(f"{path.name:24} {estado}")
            return 0

        if not pending:
            print("Sin migraciones pendientes.")
            return 0

        for path in pending:
            print(f"Aplicando {path.name} ... ", end="", flush=True)
            if dry_run:
                print("(dry-run)")
                continue
            sql = path.read_text(encoding="utf-8-sig")
            # asyncpg no acepta multiples sentencias en execute(); se usa el
            # protocolo simple mediante una transaccion explicita por archivo.
            async with conn.transaction():
                await conn.execute(sql)
                await conn.execute(
                    "insert into private.schema_migrations (version, checksum) values ($1, $2)",
                    path.name,
                    checksum(path),
                )
            print("ok")
        return 0
    finally:
        await conn.close()


def main() -> int:
    load_env(ROOT / ".env")
    parser = argparse.ArgumentParser(description="Aplica las migraciones SQL de CosTamarindo")
    parser.add_argument("--dry-run", action="store_true", help="no ejecuta nada")
    parser.add_argument("--status", action="store_true", help="muestra el estado")
    args = parser.parse_args()
    return asyncio.run(run(args.dry_run, args.status))


if __name__ == "__main__":
    sys.exit(main())
