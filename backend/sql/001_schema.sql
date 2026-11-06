-- CosTamarindo 2D - esquema base (DB-02)
-- Proyecto: Supabase Cloud, Postgres 17
--
-- Regla de la division DB: el cliente nunca escribe ni lee las tablas.
-- Solo el backend con la clave secreta (rol service_role) accede a estos objetos.
-- Las politicas y los privilegios se aplican en 002_rls.sql.
--
-- Idempotente: se puede aplicar dos veces sin efectos secundarios.

begin;

-- ---------------------------------------------------------------------------
-- profiles
-- ---------------------------------------------------------------------------
create table if not exists public.profiles (
    id          uuid primary key references auth.users (id) on delete cascade,
    username    text not null,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now(),
    constraint profiles_username_format check (username ~ '^[A-Za-z0-9_]{3,16}$')
);

-- Unicidad sin distinguir mayusculas: "Ana" y "ana" son el mismo jugador.
create unique index if not exists profiles_username_lower_key
    on public.profiles (lower(username));

comment on table public.profiles is
    'Perfil publico del jugador. Lo crea el backend despues de registrar la identidad en Supabase Auth.';
comment on column public.profiles.username is
    'Alias visible en el leaderboard. Formato ^[A-Za-z0-9_]{3,16}$ y unico sin distinguir mayusculas.';

-- ---------------------------------------------------------------------------
-- game_sessions
-- ---------------------------------------------------------------------------
create table if not exists public.game_sessions (
    id          uuid primary key default gen_random_uuid(),
    user_id     uuid not null references auth.users (id) on delete cascade,
    nonce       text not null,
    created_at  timestamptz not null default now(),
    expires_at  timestamptz not null,
    used_at     timestamptz,
    constraint game_sessions_nonce_format check (nonce ~ '^[A-Za-z0-9_-]{16,64}$'),
    constraint game_sessions_expiry check (expires_at > created_at)
);

create index if not exists game_sessions_user_created_idx
    on public.game_sessions (user_id, created_at desc);
create index if not exists game_sessions_pending_expiry_idx
    on public.game_sessions (expires_at) where used_at is null;

comment on table public.game_sessions is
    'Sesiones de partida emitidas por el backend. Una sesion sirve para un solo envio de puntaje.';
comment on column public.game_sessions.nonce is
    'Nonce publico de la sesion. Tambien entra en la cadena canonica firmada.';
comment on column public.game_sessions.used_at is
    'Momento en que la sesion se consumio. Un segundo envio con la misma sesion se rechaza.';

-- La clave HMAC no se guarda: el backend la deriva con SESSION_MASTER_KEY
-- a partir de (id, nonce). Si la base se filtra, no se puede firmar un puntaje.

-- ---------------------------------------------------------------------------
-- scores
-- ---------------------------------------------------------------------------
create table if not exists public.scores (
    id           bigint generated always as identity primary key,
    user_id      uuid not null references auth.users (id) on delete cascade,
    session_id   uuid not null constraint scores_session_unique unique
                 references public.game_sessions (id) on delete restrict,
    score        integer not null,
    duration_ms  integer not null,
    catches      integer not null,
    created_at   timestamptz not null default now(),
    constraint scores_value_range check (score between 0 and 100000),
    constraint scores_duration_range check (duration_ms between 1000 and 900000),
    constraint scores_catches_range check (catches between 0 and 5000)
);

create index if not exists scores_best_idx
    on public.scores (user_id, score desc);
create index if not exists scores_ranking_idx
    on public.scores (score desc, created_at asc);

comment on table public.scores is
    'Puntajes aceptados por el backend tras validar JWT, firma HMAC, nonce, TTL y plausibilidad.';
comment on constraint scores_session_unique on public.scores is
    'Una sesion de juego produce como maximo un puntaje.';

-- ---------------------------------------------------------------------------
-- login_attempts
-- ---------------------------------------------------------------------------
create table if not exists public.login_attempts (
    username_lower  text primary key,
    failed_count    integer not null default 0,
    locked_until    timestamptz,
    last_attempt_at timestamptz not null default now(),
    constraint login_attempts_failed_nonneg check (failed_count >= 0),
    constraint login_attempts_username_len check (char_length(username_lower) between 3 and 16)
);

comment on table public.login_attempts is
    'Contador de intentos fallidos por usuario para el bloqueo progresivo (A.8.5). Persistido para sobrevivir reinicios.';

-- ---------------------------------------------------------------------------
-- leaderboard
-- ---------------------------------------------------------------------------
-- Vista de consulta para administracion y reportes. No se concede a anon ni a
-- authenticated: el leaderboard sale por la API, que decide que se publica.
create or replace view public.leaderboard
with (security_invoker = true) as
select
    p.username,
    max(s.score) as best_score,
    count(s.id)  as games_played
from public.scores s
join public.profiles p on p.id = s.user_id
group by p.username
order by best_score desc;

-- Top N mas la posicion propia en una sola llamada.
-- La vista anterior no puede resolver "mi posicion" porque no conoce al llamante.
create or replace function public.get_leaderboard(
    p_user_id uuid default null,
    p_limit   integer default 5
)
returns table (
    rank_position bigint,
    username   text,
    best_score integer,
    is_me      boolean
)
language sql
stable
security definer
set search_path = public, pg_temp
as $$
    with best as (
        select s.user_id, max(s.score) as best_score
        from public.scores s
        group by s.user_id
    ),
    ranked as (
        select
            b.user_id,
            p.username,
            b.best_score,
            row_number() over (order by b.best_score desc, b.user_id) as rank_position
        from best b
        join public.profiles p on p.id = b.user_id
    )
    select r.rank_position, r.username, r.best_score, r.user_id = p_user_id
    from ranked r
    where r.rank_position <= greatest(p_limit, 1)
       or r.user_id = p_user_id
    order by r.rank_position;
$$;

comment on function public.get_leaderboard(uuid, integer) is
    'Devuelve el top N y, si se pasa p_user_id, tambien la fila del jugador con su posicion.';

commit;

