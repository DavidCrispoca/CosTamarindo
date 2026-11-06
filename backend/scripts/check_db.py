"""Comprobaciones de la division DB (DB-03, DB-04, DB-05).

Verifica contra la base real:
  - tablas y RLS habilitado
  - ausencia de politicas para anon/authenticated
  - privilegios efectivos de los roles de cliente
  - funcion del leaderboard

Uso:
    python backend/scripts/check_db.py
Salida: texto legible y codigo de salida != 0 si algo no cumple.
"""

from __future__ import annotations

import asyncio
import os
import sys
from pathlib import Path

import asyncpg

ROOT = Path(__file__).resolve().parent.parent.parent


def load_env(path: Path) -> None:
    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip())


async def main() -> int:
    load_env(ROOT / ".env")
    url = os.environ.get("SUPABASE_DB_URL") or os.environ.get("SUPABASE_DB_URL")
    if not url:
        print("Falta SUPABASE_DB_URL")
        return 2

    conn = await asyncpg.connect(url, ssl="require", statement_cache_size=0)
    failures: list[str] = []
    try:
        print("== Tablas de public ==")
        rows = await conn.fetch(
            """
            select c.relname as name, c.relkind::text as kind, c.relrowsecurity as rls, c.relforcerowsecurity as forced
            from pg_class c
            join pg_namespace n on n.oid = c.relnamespace
            where n.nspname = 'public' and c.relkind in ('r', 'v')
            order by c.relkind, c.relname
            """
        )
        for row in rows:
            print(f"  {row['name']:18} kind={row['kind']} rls={row['rls']} forced={row['forced']}")

        expected = {"profiles", "game_sessions", "scores", "login_attempts"}
        present = {r["name"] for r in rows if r["kind"] == "r"}
        missing = expected - present
        if missing:
            failures.append(f"faltan tablas: {sorted(missing)}")

        for row in rows:
            if row["kind"] == "r" and row["name"] in expected and not row["rls"]:
                failures.append(f"{row['name']} tiene RLS deshabilitado")

        print("== Politicas RLS ==")
        policies = await conn.fetch(
            "select tablename, policyname from pg_policies where schemaname = 'public' order by 1"
        )
        if not policies:
            print("  ninguna (deny by default: es lo esperado)")
        else:
            for row in policies:
                print(f"  {row['tablename']}.{row['policyname']}")

        print("== Privilegios de anon/authenticated ==")
        grants = await conn.fetch(
            """
            select grantee, table_name, string_agg(privilege_type, ',' order by privilege_type) as privs
            from information_schema.role_table_grants
            where table_schema = 'public' and grantee in ('anon', 'authenticated')
            group by grantee, table_name
            order by 1, 2
            """
        )
        if not grants:
            print("  sin privilegios de tabla (correcto)")
        else:
            for row in grants:
                print(f"  {row['grantee']} -> {row['table_name']}: {row['privs']}")
                failures.append(f"{row['grantee']} conserva privilegios sobre {row['table_name']}")

        usage = await conn.fetch(
            """
            select r.rolname, has_schema_privilege(r.rolname, 'public', 'USAGE') as usage
            from pg_roles r where r.rolname in ('anon', 'authenticated', 'service_role')
            order by 1
            """
        )
        print("== USAGE sobre el schema public ==")
        for row in usage:
            print(f"  {row['rolname']}: usage={row['usage']}")
            if row["rolname"] in ("anon", "authenticated") and row["usage"]:
                failures.append(f"{row['rolname']} conserva USAGE sobre public")

        print("== Funcion del leaderboard ==")
        fn = await conn.fetchval(
            "select pg_get_function_identity_arguments(p.oid) from pg_proc p "
            "join pg_namespace n on n.oid = p.pronamespace "
            "where n.nspname = 'public' and p.proname = 'get_leaderboard'"
        )
        print(f"  get_leaderboard({fn})")
        if fn is None:
            failures.append("no existe get_leaderboard")
        else:
            rows = await conn.fetch("select * from public.get_leaderboard(null, 5)")
            print(f"  filas devueltas con la base vacia: {len(rows)}")

        print("== Constraints clave ==")
        cons = await conn.fetch(
            """
            select conrelid::regclass::text as tabla, conname
            from pg_constraint
            where connamespace = 'public'::regnamespace and contype in ('c', 'u', 'f')
            order by 1, 2
            """
        )
        for row in cons:
            print(f"  {row['tabla']}: {row['conname']}")
    finally:
        await conn.close()

    if failures:
        print("\nFALLOS:")
        for item in failures:
            print(f"  - {item}")
        return 1
    print("\nTodo en orden.")
    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
