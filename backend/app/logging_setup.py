"""Logs JSON a stdout.

Formato pensado para que Wazuh los ingiera sin parser propio:
    {"ts":"2026-11-06T23:10:00.123Z","level":"INFO","event":"score_accepted",...}

A.8.12: no se registran PIN, tokens ni prompts completos. La IP se enmascara
(ultimo octeto a cero) para reducir datos personales en los logs.
"""

from __future__ import annotations

import json
import logging
import sys
import uuid
from contextvars import ContextVar
from datetime import datetime, timezone

request_id_var: ContextVar[str] = ContextVar("request_id", default="-")
user_id_var: ContextVar[str] = ContextVar("user_id", default="-")


def mask_ip(ip: str | None) -> str:
    if not ip:
        return "-"
    if ":" in ip:  # IPv6
        parts = ip.split(":")
        return ":".join(parts[:3] + ["****"])
    parts = ip.split(".")
    if len(parts) == 4:
        return ".".join(parts[:3] + ["0"])
    return "-"


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload: dict[str, object] = {
            "ts": datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z"),
            "level": record.levelname,
            "logger": record.name,
            "event": getattr(record, "event", record.getMessage()),
            "request_id": request_id_var.get(),
        }
        user_id = user_id_var.get()
        if user_id != "-":
            payload["user_id"] = user_id
        for key, value in getattr(record, "fields", {}).items():
            payload[key] = value
        if record.exc_info:
            payload["error"] = self.formatException(record.exc_info)
        return json.dumps(payload, ensure_ascii=False, default=str)


def configure_logging(level: str = "INFO") -> None:
    root = logging.getLogger()
    root.handlers.clear()
    handler = logging.StreamHandler(sys.stdout)
    handler.setFormatter(JsonFormatter())
    root.addHandler(handler)
    root.setLevel(level.upper())
    # Uvicorn trae sus propios handlers; se reemplazan por el JSON.
    for name in ("uvicorn", "uvicorn.error", "uvicorn.access"):
        logger = logging.getLogger(name)
        logger.handlers.clear()
        logger.propagate = True


def audit(event: str, **fields: object) -> None:
    """Evento de negocio o de seguridad (A.8.15)."""
    logging.getLogger("costamarindo.audit").info(event, extra={"event": event, "fields": fields})


def new_request_id() -> str:
    return uuid.uuid4().hex
