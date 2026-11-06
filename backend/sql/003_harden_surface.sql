-- CosTamarindo 2D - cierre de la superficie de datos (DB-03)
--
-- 002 dejo las tablas sin privilegios, pero el schema public seguia concediendo
-- USAGE a PUBLIC (comportamiento por defecto de Postgres). Con USAGE, PostgREST
-- puede enumerar objetos aunque no pueda leer filas. Aqui se cierra del todo y
-- se saca el registro de migraciones de public.

begin;

-- 1. El schema public deja de ser visible para cualquiera que no sea dueno o
--    service_role. pg_database_owner y postgres conservan sus privilegios.
revoke usage on schema public from public;
grant usage on schema public to service_role;

-- 2. El registro de migraciones no pertenece al schema del producto.
create schema if not exists private;
revoke all on schema private from public, anon, authenticated;
grant usage on schema private to service_role;

create table if not exists private.schema_migrations (
    version    text primary key,
    checksum   text not null,
    applied_at timestamptz not null default now()
);

alter table private.schema_migrations enable row level security;
revoke all on private.schema_migrations from public, anon, authenticated;

-- Migrar el historial si ya existia en public y quitarlo de ahi.
insert into private.schema_migrations (version, checksum, applied_at)
select version, checksum, applied_at
from public.schema_migrations
on conflict (version) do nothing;

drop table if exists public.schema_migrations;

commit;
