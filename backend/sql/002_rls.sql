-- CosTamarindo 2D - politicas, privilegios y superficie de datos (DB-03)
--
-- Postura: el cliente no lee ni escribe la base. No hay politicas RLS porque no
-- hay acceso legitimo de anon ni de authenticated: todo pasa por la API, que usa
-- el rol service_role. RLS habilitado sin politicas = denegar por defecto.
--
-- Esto tambien evita filtrar user_id y session_id por PostgREST, que es lo que
-- haria una politica de lectura publica sobre scores.

begin;

alter table public.profiles       enable row level security;
alter table public.game_sessions  enable row level security;
alter table public.scores         enable row level security;
alter table public.login_attempts enable row level security;

-- Privilegios de tabla
revoke all on public.profiles       from anon, authenticated;
revoke all on public.game_sessions  from anon, authenticated;
revoke all on public.scores         from anon, authenticated;
revoke all on public.login_attempts from anon, authenticated;

-- Secuencias
revoke all on sequence public.scores_id_seq from anon, authenticated;

-- Vistas y funciones
revoke all on public.leaderboard from anon, authenticated;
revoke all on function public.get_leaderboard(uuid, integer) from public, anon, authenticated;
grant execute on function public.get_leaderboard(uuid, integer) to service_role;

-- El schema public deja de ser visible para los roles de cliente: PostgREST no
-- puede enumerar tablas ni columnas. Si algun dia hace falta una lectura
-- directa, se concede de forma explicita y acotada.
revoke usage on schema public from anon, authenticated;
grant usage on schema public to service_role;

-- Objetos futuros: por defecto tampoco tendran permisos de cliente.
alter default privileges in schema public revoke all on tables    from anon, authenticated;
alter default privileges in schema public revoke all on sequences from anon, authenticated;
alter default privileges in schema public revoke all on functions from anon, authenticated;

commit;
