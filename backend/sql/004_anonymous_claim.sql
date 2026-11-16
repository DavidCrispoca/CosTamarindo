-- CosTamarindo 2D - puntaje anonimo y reclamo de nombre (DB-04)
--
-- El login deja de ser obligatorio antes de jugar. Se juega anonimo; si el
-- puntaje entra al top 5 global, se reclama con nombre + PIN (4-6 digitos).
--
-- Cambios:
--   - game_sessions.user_id y scores.user_id pasan a ser opcionales (anonimo).
--   - indice parcial para rankear solo los puntajes ya reclamados.
--   - get_leaderboard ya excluye los anonimos: su join con profiles descarta
--     las filas con user_id null, asi que no cambia.

begin;

alter table public.game_sessions alter column user_id drop not null;
alter table public.scores alter column user_id drop not null;

create index if not exists scores_named_best_idx
    on public.scores (score desc) where user_id is not null;

commit;
