"""Registro, login, refresh y logout.

Decisiones que se apartan del documento original y por que:

- El cliente no habla con Supabase Auth. Todo pasa por este servicio, asi el
  bloqueo progresivo (A.8.5) se puede aplicar de verdad y las claves de
  Supabase no viajan nunca al navegador.
- El backend emite su propio JWT de vida corta. GoTrue queda como proveedor de
  identidad; el token de GoTrue no se reenvia al cliente.
- El PIN se deriva con PIN_PEPPER antes de llegar a GoTrue, porque GoTrue exige
  un minimo de 6 caracteres y porque un PIN de 4 digitos no debe almacenarse
  como credencial directa.
"""

from __future__ import annotations

import uuid

from ..config import Settings
from ..errors import AccountLocked, InvalidCredentials, InvalidToken, UsernameTaken, WeakPin
from ..logging_setup import audit, user_id_var
from ..models import TokenPair
from ..providers.supabase_auth import AuthRejected, SupabaseAuth
from ..repositories import Profile, Repository
from ..security import create_access_token, derive_pin_password, is_weak_pin


def _token_pair(settings: Settings, profile: Profile) -> TokenPair:
    token, expires_in = create_access_token(
        secret=settings.backend_jwt_secret,
        user_id=profile.id,
        username=profile.username,
        env=settings.env,
        ttl_minutes=settings.backend_jwt_ttl_minutes,
    )
    return TokenPair(
        access_token=token,
        refresh_token="",  # se rellena con el de GoTrue
        expires_in=expires_in,
        user_id=profile.id,
        username=profile.username,
    )


class AuthService:
    def __init__(self, repo: Repository, provider: SupabaseAuth, settings: Settings) -> None:
        self._repo = repo
        self._provider = provider
        self._settings = settings

    async def register(self, *, username: str, pin: str) -> TokenPair:
        if is_weak_pin(pin):
            raise WeakPin()

        existing = await self._repo.get_profile_by_username(username)
        if existing:
            raise UsernameTaken()

        password = derive_pin_password(pin, username, self._settings.pin_pepper)
        try:
            user_id = await self._provider.create_user(username=username, password=password)
        except AuthRejected as exc:
            audit("register_rejected", username=username, reason="auth_rejected")
            raise UsernameTaken() from exc

        try:
            await self._repo.create_profile(user_id, username)
        except Exception:
            # Compensacion: si el perfil no se crea, no dejamos una identidad
            # huerfana en Supabase Auth.
            await self._provider.delete_user(user_id)
            audit("register_failed", username=username, user_id=str(user_id), reason="profile_insert")
            raise

        audit("register_ok", user_id=str(user_id), username=username)
        tokens = await self._provider.password_grant(username=username, password=password)
        pair = _token_pair(self._settings, Profile(id=user_id, username=username))
        if tokens:
            pair.refresh_token = tokens.refresh_token
        return pair

    async def login(self, *, username: str, pin: str) -> TokenPair:
        username_lower = username.strip().lower()
        locked = await self._repo.locked_until(username_lower)
        if locked:
            audit("login_blocked", username=username)
            raise AccountLocked()

        password = derive_pin_password(pin, username, self._settings.pin_pepper)
        tokens = await self._provider.password_grant(username=username, password=password)
        if tokens is None:
            lock_until = await self._repo.register_login_failure(
                username_lower,
                self._settings.login_max_attempts,
                self._settings.login_lock_seconds,
            )
            audit(
                "login_fail",
                username=username,
                locked=bool(lock_until),
                lock_seconds=self._settings.login_lock_seconds if lock_until else 0,
            )
            if lock_until:
                raise AccountLocked()
            raise InvalidCredentials()

        await self._repo.clear_login_failures(username_lower)
        profile = await self._repo.get_profile(tokens.user_id)
        if profile is None:
            # Autocuracion: identidad sin perfil (por ejemplo si fallo la
            # compensacion en un registro anterior).
            await self._repo.create_profile(tokens.user_id, username)
            profile = Profile(id=tokens.user_id, username=username)

        audit("login_ok", user_id=str(profile.id), username=profile.username)
        pair = _token_pair(self._settings, profile)
        pair.refresh_token = tokens.refresh_token
        return pair

    async def refresh(self, *, refresh_token: str) -> TokenPair:
        tokens = await self._provider.refresh(refresh_token)
        if tokens is None:
            raise InvalidToken("refresh_token_invalido")
        profile = await self._repo.get_profile(tokens.user_id)
        if profile is None:
            raise InvalidToken("perfil_inexistente")
        pair = _token_pair(self._settings, profile)
        pair.refresh_token = tokens.refresh_token
        audit("token_refresh", user_id=str(profile.id))
        return pair

    async def logout(self, *, username: str) -> None:
        """Cierre de sesion del lado del cliente.

        No se revoca en GoTrue porque el access token de GoTrue nunca sale del
        backend y el refresh rota en cada uso. La sesion del juego caduca por TTL
        (30 min) y el envio de puntaje exige ademas la clave HMAC de la partida.
        """
        audit("logout", username=username, user_id=user_id_var.get())
