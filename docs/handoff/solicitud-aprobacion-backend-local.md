# Solicitud de aprobación — backend local y entrega segura de secretos

> **Para:** Santiago y su agente.
> **De:** equipo de Yoann.
> **Fecha:** 2026-11-25.
> **Base:** `main` / `Yoann` en `b762298`.
> **Objetivo:** pedir aprobación para levantar un stack local de desarrollo
> (`sg-ai` + `sg-backend` + `sg-frontend`) contra el mismo Supabase DEV, y acordar cómo se
> entregan los secretos que faltan.
>
> **Regla de oro:** este documento **no contiene secretos**. No pegues claves, tokens,
> contraseñas ni cadenas de conexión en este archivo, en el repositorio, en el chat ni en capturas.
> Para los valores reales, usa el canal seguro de la sección 4.

---

## 1. Estado actual

Ya está resuelto y **no hace falta volver a enviarlo**:

- `SUPABASE_URL`: `https://jukyiwsteuzwrmzlgfme.supabase.co`.
- `SUPABASE_PROJECT_REF`: `jukyiwsteuzwrmzlgfme`.
- `SUPABASE_PUBLISHABLE_KEY`: **recibida** (clave que empieza por `sb_publishable_`).
- Proyecto Supabase DEV en `us-east-1`, PostgreSQL 17.11.
- Migraciones `001`–`004` aplicadas; `004_anonymous_claim.sql` aplicada el 2026-11-16.
- `check_db.py` en verde; RLS y privilegios correctos.
- `costamarindo-backend:latest` reconstruida y validada en el nodo de Santiago.
- E2E real en verde y evidencias `F7/16` y `F7/17`.

Lo que falta para reproducir el stack en el equipo de Yoann:

| Variable | Estado | Observación |
| :--- | :--- | :--- |
| `SUPABASE_SECRET_KEY` | ❌ falta | Debe ser `sb_secret_...`; solo va al backend |
| `SUPABASE_DB_URL` | ❌ falta | Pooler de **sesión**, puerto `5432`, con contraseña |
| `BACKEND_JWT_SECRET` | ❌ falta | 32+ caracteres |
| `PIN_PEPPER` | ❌ falta | 16+ caracteres |
| `SESSION_MASTER_KEY` | ❌ falta | 16+ caracteres |
| `SUPABASE_URL` / ref | ✅ conocido | No hace falta pedirlo de nuevo |
| `SUPABASE_PUBLISHABLE_KEY` | ✅ recibida | No hace falta pedirlo de nuevo |

> La publishable key no alcanza: el backend usa la **secret key** para crear/borrar usuarios en
> GoTrue y la **DB URL** para sesiones, puntajes, claim y leaderboard. Sin esos valores no arranca.

---

## 2. Aprobación que necesitamos

### 2.1 Levantar backend local contra Supabase DEV

Pedimos autorización explícita para:

- Levantar `sg-ai` (Ollama `qwen2.5:3b`) y `sg-backend` en el equipo de Yoann.
- Usar el mismo proyecto **Supabase DEV** `jukyiwsteuzwrmzlgfme`.
- Levantar `sg-frontend` localmente para probar el flujo `Login → Game → claim → NPC`.
- No publicar `8000` ni `11434`; solo `443`/`8081` en local.
- No tocar PROD, no crear otro proyecto y no reaplicar `004`.

**Preguntas para Santiago:**

- [ ] ¿Apruebas una segunda instancia local de `sg-backend` contra el mismo DEV?
- [ ] ¿La instancia oficial sigue siendo la de Santiago, o la local pasa a ser la principal?
- [ ] ¿Hay ventana de mantenimiento, límite de concurrencia o alguna restricción por tener dos
      backends contra la misma base?
- [ ] ¿Quién será el responsable del backend para la entrega final?

### 2.2 Secretos propios del backend

Los tres secretos `BACKEND_JWT_SECRET`, `PIN_PEPPER` y `SESSION_MASTER_KEY` ya existen en el
`.env` del nodo de Santiago. Para no invalidar PINs, claims y sesiones existentes, lo recomendable
es usar **los mismos valores** en el equipo de Yoann.

- [ ] ¿Autorizas compartir esos tres valores por el canal seguro?
- [ ] Si prefieres no compartirlos, ¿autorizas generarlos localmente?
- [ ] Si se generan nuevos, ¿aceptas que los PINs ya reclamados no funcionen desde la instancia
      local? `PIN_PEPPER` distinto cambia la contraseña derivada de todos los usuarios.

### 2.3 Pruebas E2E y datos

El script `backend/scripts/e2e_check.py`:

- Crea un usuario sintético, lo usa y lo borra.
- Crea una sesión anónima y un puntaje de prueba.
- Deja un score anónimo de DEV (sin perfil ni datos personales).

- [ ] ¿Autorizas ejecutar el E2E contra el mismo Supabase DEV?
- [ ] ¿Prefieres una ventana específica o que se use un usuario/proyecto de pruebas separado?
- [ ] ¿Autorizas limpiar los datos de prueba al terminar?

### 2.4 Tailscale / red privada

**Recomendación: no crear la tailnet ahora.**

- El stack local usa redes Docker internas: `sg-frontend → sg-backend → sg-ai`.
- Tailscale no reemplaza TLS, JWT ni los controles de seguridad; solo sería administración o
  transporte entre máquinas.
- Solo sería necesario si:
  - necesitamos entrar remotamente al nodo de Santiago o a su `https://juego.local`;
  - vamos a operar los cinco nodos en hosts distintos;
  - Security necesita administrar la VM Wazuh desde fuera.

- [ ] ¿Estás de acuerdo en no usar Tailscale para esta fase?
- [ ] Si quieres usarla más adelante, ¿qué nodos y qué ACL propones?
- [ ] Si se usa: sin exit node, sin publicar `8000`/`11434`, solo tailnet y con ACL.

---

## 3. Cómo debe tratar la información tu agente

Estas reglas son obligatorias para el agente de Santiago y para cualquier persona que prepare la
respuesta.

### 3.1 Qué NO hacer

- **No envíes secretos por el chat** de OpenCode, Discord, WhatsApp, Telegram, Slack, correo sin
  cifrar ni por el chat del agente.
- **No pegues valores** en este `.md`, en `README.md`, `Plan.md`, `contexto.md`, handoffs,
  evidencias, issues, PRs, comentarios de GitHub, gists ni wikis.
- **No los incluyas en commits**, ramas, logs, capturas de pantalla, videos ni archivos
  compartidos sin cifrar.
- **No envíes** `SUPABASE_SECRET_KEY`, `SUPABASE_DB_URL`, `BACKEND_JWT_SECRET`, `PIN_PEPPER` ni
  `SESSION_MASTER_KEY` como texto plano.
- **No envíes capturas del `.env`** ni fragmentos de valores.
- **No uses ni reenvíes** un PAT `sbp_...` que se haya compartido: es un token de cuenta, no una
  clave de proyecto. Debe revocarse en Supabase y no se usará para leer API keys.
- Si un secreto ya se expuso, **rótalo antes de continuar**; no lo reutilices.

### 3.2 Qué SÍ hacer

Elige **una** de estas vías para los valores reales:

1. **Gestor de secretos compartido (recomendado):** 1Password, Bitwarden o el vault del proyecto.
   Crear una entrada llamada `CosTamarindo DEV — Yoann local` con las variables pendientes y dar
   acceso al equipo de Yoann. Enviar por chat **solo el nombre de la entrada**, nunca los valores.
2. **Carga directa en el equipo de Yoann**, solo si hay acceso autorizado: cargar el `.env` local
   sin mostrarlo y confirmar únicamente qué variables quedaron definidas.
3. **Archivo cifrado fuera del repositorio:** age/GPG/7z con AES-256; la contraseña se comparte por
   un canal distinto y no se escribe en el mismo mensaje.

Para lo que **no es secreto** sí puedes usar el canal normal:

- Aprobaciones, decisiones, ventanas de mantenimiento y responsables: responde en este `.md`, en un
  issue/PR o en el chat.
- Salidas de `docker compose ps`, `/api/health`, `migrate.py --status` y `check_db.py`: pégalas
  **redactadas**, sin claves ni cadenas de conexión.
- Evidencias y capturas sin secretos: déjalas en `evidencias/F7/` o en una rama.

### 3.3 Formato de confirmación de secretos

En la respuesta, escribe solo algo como:

```text
SUPABASE_SECRET_KEY: entregada por vault, entrada "CosTamarindo DEV — Yoann local"
SUPABASE_DB_URL: entregada por vault, entrada "CosTamarindo DEV — Yoann local"
BACKEND_JWT_SECRET: mismo valor del nodo / generado localmente con autorización
PIN_PEPPER: mismo valor del nodo / generado localmente con autorización
SESSION_MASTER_KEY: mismo valor del nodo / generado localmente con autorización
```

Nunca escribas el valor real.

---

## 4. Plantilla de respuesta

Santiago o su agente pueden responder así:

```text
Aprobación de backend local: SÍ / NO
Instancia oficial: Santiago / Yoann / otra
Mismo Supabase DEV: SÍ / NO
Mismo BACKEND_JWT_SECRET: SÍ / NO
Mismo PIN_PEPPER: SÍ / NO
Mismo SESSION_MASTER_KEY: SÍ / NO
E2E autorizado: SÍ / NO
Ventana o restricciones:
Tailscale ahora: NO / SÍ, porque...
Secretos entregados por: vault / carga directa / archivo cifrado
Nombre de la entrada o ubicación segura:
Responsable del backend final:
Observaciones:
```

---

## 5. Criterio de aceptación

Esta solicitud queda aprobada cuando exista una respuesta que confirme:

- [ ] Autorización para levantar el backend local contra DEV.
- [ ] Decisión sobre los tres secretos propios.
- [ ] Autorización del E2E y limpieza de datos.
- [ ] Entrega segura de `SUPABASE_SECRET_KEY` y `SUPABASE_DB_URL`.
- [ ] Confirmación de que no se enviarán secretos por chat, repo ni capturas.
- [ ] Decisión sobre Tailscale.

Con eso podemos levantar el stack local, ejecutar el E2E real y continuar con la publicación y las
evidencias finales sin exponer credenciales.
