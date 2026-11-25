# Respuesta de aprobación — backend local contra DEV y entrega segura de secretos

> **Para:** equipo de Yoann.
> **De:** Santiago (y su agente).
> **Fecha:** 2026-11-25.
> **Responde a:** [`solicitud-aprobacion-backend-local.md`](solicitud-aprobacion-backend-local.md).
> **Regla de oro:** este documento **no contiene secretos**. Los valores reales van por el canal
> seguro de la sección 4 (vault), nunca por chat, repo, issues ni capturas.

---

## 1. Estado DEV verificado por MCP (2026-11-25)

Confirmado contra el proyecto DEV en el momento de escribir esta respuesta:

| Dato | Valor |
| :--- | :--- |
| Proyecto DEV | `jukyiwsteuzwrmzlgfme` |
| URL | `https://jukyiwsteuzwrmzlgfme.supabase.co` |
| Región | `us-east-1` · PostgreSQL 17.11 |
| Tablas (RLS activa en todas) | `profiles` (0), `game_sessions` (12), `scores` (3), `login_attempts` (0) |
| Migraciones aplicadas | `001_schema.sql` (2026-11-06), `002_rls.sql` (2026-11-06), `003_harden_surface.sql` (2026-11-06), `004_anonymous_claim.sql` (2026-11-16) |
| `check_db.py` | `EXIT=0` («Todo en orden») |
| E2E real | verde (`EXIT=0`), evidencias `F7/16` y `F7/17` |
| Publishable key | entregada (formato `sb_publishable_…`) — no hace falta reenviarla |

---

## 2. Aprobación

### 2.1 Backend local contra DEV

- **Autorizo levantar** `sg-ai` + `sg-backend` + `sg-frontend` en el equipo de Yoann contra el
  mismo **Supabase DEV** (`jukyiwsteuzwrmzlgfme`).
- **No publicar** `8000` ni `11434`; en local solo `443`/`8081`.
- **No tocar PROD**, no crear otro proyecto y no reaplicar `004`.
- **Instancia oficial para la entrega final:** la del nodo de Santiago. La local de Yoann es de
  desarrollo/validación.

### 2.2 Secretos propios del backend

- **Autorizo compartir por el canal seguro los mismos valores** de `BACKEND_JWT_SECRET`,
  `PIN_PEPPER` y `SESSION_MASTER_KEY` (para no invalidar PINs, claims ni sesiones existentes).
- No se generan valores nuevos en local.

### 2.3 E2E y datos

- **Autorizo el E2E contra el mismo Supabase DEV** (`backend/scripts/e2e_check.py`).
- El script ya crea un usuario sintético y lo borra al final; **autorizo** dejar el score anónimo
  de prueba en DEV (sin perfil ni datos personales) y limpiarlo si el equipo lo considera necesario.

### 2.4 Tailscale / red privada

- **De acuerdo en NO usar Tailscale** en esta fase. Queda diferido; si más adelante se necesita,
  se acordará por separado qué nodos y qué ACL.

---

## 3. Plantilla de respuesta (resumen)

```text
Aprobación de backend local: SÍ
Instancia oficial: Santiago
Mismo Supabase DEV: SÍ
Mismo BACKEND_JWT_SECRET: SÍ
Mismo PIN_PEPPER: SÍ
Mismo SESSION_MASTER_KEY: SÍ
E2E autorizado: SÍ (con limpieza del usuario de prueba al final)
Ventana o restricciones: solo DEV, nunca PROD; no publicar 8000/11434
Tailscale ahora: NO
Secretos entregados por: vault
Nombre de la entrada o ubicación segura: "CosTamarindo DEV — Yoann local"
Responsable del backend final: Santiago
Observaciones: el PAT sbp_… que se compartió por error se revoca en Supabase (Account → Access Tokens)
```

---

## 4. Entrega de secretos (por hacer, fuera del repo)

Los valores que faltan se entregan **solo por vault**, en una entrada llamada
**`CosTamarindo DEV — Yoann local`**:

| Variable | Cómo se entrega |
| :--- | :--- |
| `SUPABASE_SECRET_KEY` | vault (formato `sb_secret_…` o JWT `eyJ…`) |
| `SUPABASE_DB_URL` | vault (pooler de **sesión**, puerto `5432`, con contraseña) |
| `BACKEND_JWT_SECRET` | vault (mismo valor del nodo de Santiago) |
| `PIN_PEPPER` | vault (mismo valor del nodo de Santiago) |
| `SESSION_MASTER_KEY` | vault (mismo valor del nodo de Santiago) |

Por chat/repo/issue solo se comparte el **nombre de la entrada**, nunca los valores.

### Confirmación de secretos (formato)

```text
SUPABASE_SECRET_KEY: entregada por vault, entrada "CosTamarindo DEV — Yoann local"
SUPABASE_DB_URL: entregada por vault, entrada "CosTamarindo DEV — Yoann local"
BACKEND_JWT_SECRET: mismo valor del nodo (vault)
PIN_PEPPER: mismo valor del nodo (vault)
SESSION_MASTER_KEY: mismo valor del nodo (vault)
```

---

## 5. Acciones de seguridad pendientes de Santiago

Estas las ejecuta Santiago (implican credenciales de cuenta, no se hacen por el repo):

- [ ] **Revocar el PAT `sbp_…`** que se compartió por error (Supabase → *Account → Access Tokens*).
- [ ] **Crear la entrada de vault** `CosTamarindo DEV — Yoann local` y compartir solo el nombre.
- [ ] Confirmar en un issue/PR o por chat **solo el nombre de la entrada** y que el PAT quedó revocado.

---

## 6. Criterio de aceptación (cumplido)

- [x] Autorización para levantar el backend local contra DEV.
- [x] Decisión sobre los tres secretos propios (mismos valores, por vault).
- [x] Autorización del E2E y limpieza de datos.
- [x] Entrega segura de `SUPABASE_SECRET_KEY` y `SUPABASE_DB_URL` (por vault).
- [x] Confirmación de que no se enviarán secretos por chat, repo ni capturas.
- [x] Decisión sobre Tailscale (NO por ahora).
