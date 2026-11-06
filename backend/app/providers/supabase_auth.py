"""Cliente minimo de Supabase Auth (GoTrue).

Se habla HTTP directo en lugar de usar el SDK para dejar explicito que:
  - la clave secreta nunca sale del servidor;
  - GoTrue rechaza la clave secreta si el User-Agent parece un navegador, por
    eso se declara uno de servicio;
  - el cliente del juego nunca habla con Supabase: solo con esta API.
"""

from __future__ import annotations

import uuid
from dataclasses import dataclass

import httpx

from ..errors import UpstreamError

USER_AGENT = "CostamarindoBackend/1.0 (+https://juego.local)"
PLAYERS_DOMAIN = "players.costamarindo.local"


class AuthRejected(Exception):
    """GoTrue rechazo la peticion (credenciales, usuario duplicado, token)."""


@dataclass(frozen=True, slots=True)
class AuthTokens:
    access_token: str
    refresh_token: str
    expires_in: int
    user_id: uuid.UUID


def synthetic_email(username: str) -> str:
    return f"{username.strip().lower()}@{PLAYERS_DOMAIN}"


class SupabaseAuth:
    def __init__(self, *, base_url: str, publishable_key: str, secret_key: str, timeout: float = 15.0) -> None:
        self._base = base_url.rstrip("/")
        self._publishable = publishable_key
        self._secret = secret_key
        self._timeout = timeout

    # -- helpers ----------------------------------------------------------
    def _admin_headers(self) -> dict[str, str]:
        return {
            "apikey": self._secret,
            "Authorization": f"Bearer {self._secret}",
            "User-Agent": USER_AGENT,
            "Content-Type": "application/json",
        }

    def _public_headers(self) -> dict[str, str]:
        return {
            "apikey": self._publishable,
            "User-Agent": USER_AGENT,
            "Content-Type": "application/json",
        }

    async def _post(self, path: str, *, headers: dict[str, str], json: dict) -> dict:
        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.post(f"{self._base}{path}", headers=headers, json=json)
        except httpx.HTTPError as exc:
            raise UpstreamError() from exc
        return self._parse(response)

    async def _delete(self, path: str, *, headers: dict[str, str]) -> None:
        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.delete(f"{self._base}{path}", headers=headers)
        except httpx.HTTPError as exc:
            raise UpstreamError() from exc
        if response.status_code >= 500:
            raise UpstreamError()

    @staticmethod
    def _parse(response: httpx.Response) -> dict:
        if response.status_code >= 500:
            raise UpstreamError()
        if response.status_code >= 400:
            raise AuthRejected(response.text[:200])
        if not response.content:
            return {}
        return response.json()

    # -- operaciones ------------------------------------------------------
    async def create_user(self, *, username: str, password: str) -> uuid.UUID:
        payload = {
            "email": synthetic_email(username),
            "password": password,
            "email_confirm": True,
            "user_metadata": {"username": username},
        }
        data = await self._post("/auth/v1/admin/users", headers=self._admin_headers(), json=payload)
        return uuid.UUID(data["id"])

    async def delete_user(self, user_id: uuid.UUID) -> None:
        await self._delete(f"/auth/v1/admin/users/{user_id}", headers=self._admin_headers())

    async def password_grant(self, *, username: str, password: str) -> AuthTokens | None:
        payload = {"email": synthetic_email(username), "password": password}
        try:
            data = await self._post(
                "/auth/v1/token?grant_type=password", headers=self._public_headers(), json=payload
            )
        except AuthRejected:
            return None
        return self._tokens(data)

    async def refresh(self, refresh_token: str) -> AuthTokens | None:
        try:
            data = await self._post(
                "/auth/v1/token?grant_type=refresh_token",
                headers=self._public_headers(),
                json={"refresh_token": refresh_token},
            )
        except AuthRejected:
            return None
        return self._tokens(data)

    async def revoke(self, access_token: str) -> None:
        headers = {
            "apikey": self._publishable,
            "Authorization": f"Bearer {access_token}",
            "User-Agent": USER_AGENT,
        }
        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                await client.post(f"{self._base}/auth/v1/logout", headers=headers)
        except httpx.HTTPError:
            # Un logout fallido no debe romper la sesion del cliente.
            return

    @staticmethod
    def _tokens(data: dict) -> AuthTokens:
        return AuthTokens(
            access_token=data["access_token"],
            refresh_token=data["refresh_token"],
            expires_in=int(data.get("expires_in", 3600)),
            user_id=uuid.UUID(data["user"]["id"]),
        )
