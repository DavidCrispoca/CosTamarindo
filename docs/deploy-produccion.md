# Despliegue a producción (Opción A · Oracle Cloud Always Free + Supabase)

> **Objetivo:** publicar CosTamarindo 2D con una URL pública, leaderboard real (Supabase PROD),
> login/reclamo, NPC con Ollama y jugabilidad completa, todo en planes gratuitos.
>
> **Estado:** este documento es un **runbook listo para ejecutar**. El agente no puede crear la
> cuenta de Oracle ni entrar por SSH a la VM (eso requiere credenciales que solo tiene Santiago),
> así que aquí queda todo preparado para que el despliegue sea copiar-pegar. Lo marcado
> `[validar en la VM]` no se pudo probar desde esta máquina.

---

## 0. Qué hay que pedirle a Santiago (lo único que no puede hacer el agente)

| # | Qué | Por qué no lo hace el agente |
| :--- | :--- | :--- |
| 1 | Crear la **cuenta de Oracle Cloud** (pide tarjeta solo para verificar identidad; el Always Free no cobra) | requiere datos personales + tarjeta |
| 2 | Crear la **VM Always Free** y entregar la **IP pública** y la **clave SSH** | requiere acceso a la consola de Oracle |
| 3 | Crear el **proyecto PROD de Supabase** (dashboard o CLI con PAT) | el MCP solo opera sobre el proyecto DEV conectado; no tiene "crear proyecto" |
| 4 | Un **dominio** (o aceptar un subdominio gratuito tipo `*.duckdns.org`) | decisión de Santiago |

Todo lo demás (configuración, migraciones, Docker Compose, Nginx/TLS, verificación) está en este
documento y en los archivos que lo acompañan.

---

## 1. Supabase PROD — dejarlo "con todo lo necesario"

El proyecto **DEV ya tiene todo** (verificado): 4 tablas (`profiles`, `game_sessions`, `scores`,
`login_attempts`) con RLS, la función `get_leaderboard` y las migraciones `001`–`004` aplicadas.
PROD debe ser un **proyecto distinto** (el plan gratuito permite 2), con el mismo esquema y con
`ENV=prod` en el backend.

### 1.1 Crear el proyecto PROD

1. En [supabase.com](https://supabase.com) → *New project* → organización, nombre
   `costamarindo-prod`, región a elección (us-east-1 si quieres igualar DEV) y **plan Free**.
2. Guarda la **Project ref** (el subdominio `<ref>.supabase.co`), que es pública, y los datos de
   conexión. **Las claves NO se escriben en el repo ni en este documento.**

### 1.2 Aplicar el esquema (igual que DEV)

El script `backend/scripts/migrate.py` aplica `001`–`004` a cualquier base con
`SUPABASE_DB_URL`. En la máquina que tenga acceso:

```powershell
# 1) SUPABASE_DB_URL del PROYECTO PROD, en modo SESIÓN (puerto 5432), no transacción (6543).
#    Se copia de Supabase → Project Settings → Database → Connection string → "Session pooler".
$env:SUPABASE_DB_URL = "postgresql://postgres.<PROD_REF>:<password>@aws-0-<region>.pooler.supabase.com:5432/postgres"

# 2) Aplicar y verificar (deja "aplicada" en las 4 migraciones y "Todo en orden.").
backend\.venv\Scripts\python.exe backend\scripts\migrate.py
backend\.venv\Scripts\python.exe backend\scripts\check_db.py
```

Resultado esperado: 4 migraciones `aplicada`, `check_db.py` con `EXIT=0`.

### 1.3 Variables del backend en PROD

Ver [`deploy/env.prod.example`](deploy/env.prod.example). Los valores de PROD cambian respecto a DEV:

| Variable | Valor en PROD |
| :--- | :--- |
| `ENV` | `prod` |
| `SUPABASE_URL` | `https://<PROD_REF>.supabase.co` |
| `SUPABASE_PROJECT_REF` | `<PROD_REF>` |
| `SUPABASE_PUBLISHABLE_KEY` / `SUPABASE_SECRET_KEY` / `SUPABASE_DB_URL` | los de PROD |
| `CORS_ALLOWED_ORIGINS` | `https://<tu-dominio>` (la URL pública) |
| `DEV_PROJECT_REFS` | `jukyiwsteuzwrmzlgfme` (ref de DEV, para que A.8.31 bloquee PROD apuntando a DEV) |

Los secretos (`BACKEND_JWT_SECRET`, `PIN_PEPPER`, `SESSION_MASTER_KEY`) se generan nuevos en PROD:
`openssl rand -hex 32` (no reutilizar los de DEV).

> **Nota free tier:** un proyecto gratuito **se pausa tras ~1 semana sin uso**. Se reactiva con un
> clic en el dashboard (o se configura un ping de keep-alive). Para una demo, tenlo presente.

---

## 2. Oracle Cloud Always Free — crear la VM

1. Entra en [cloud.oracle.com](https://cloud.oracle.com) → *Compute → Instances → Create*.
2. **Shape:** `VM.Standard.A1.Flex` (Arm) con **4 OCPU / 24 GB RAM** (es el Always Free). Con
   24 GB sobra para Ollama (`qwen2.5:3b` ~3 GB) + backend + Nginx.
3. **Imagen:** Ubuntu 22.04 o 24.04 LTS.
4. **Clave SSH:** sube tu clave pública (o genera una en el sitio y descarga la privada).
5. **Red / Seguridad:** abre los puertos **80/tcp** y **443/tcp** en la *Ingress Rule* del VCN
   (y 22/tcp para SSH, restringido a tu IP si puedes).
6. Reserva la **IP pública** (la "ephemeral" cambia al reiniciar; mejor **reserved public IP**).

## 3. Preparar el servidor (una sola vez)

```bash
# Acceso
ssh -i tu-clave.pem ubuntu@<IP_PUBLICA>

# Docker + Compose
sudo apt-get update && sudo apt-get install -y ca-certificates curl
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER   # cierra sesión y vuelve a entrar
sudo apt-get install -y docker-compose-plugin git
```

## 4. Subir el proyecto y cargar secretos

```bash
git clone https://github.com/DavidCrispoca/CosTamarindo.git
cd CosTamarindo

# Build WebGL (o copia builds/webgl desde tu máquina si no tienes Unity en la VM).
# Si no quieres compilar en la VM, copia la carpeta builds/webgl con scp:
#   scp -r builds/webgl ubuntu@<IP>:CosTamarindo/builds/

# Secretos SOLO en el servidor (el archivo .env no se versiona):
cp deploy/env.prod.example .env
nano .env    # rellena con los valores de PROD (Supabase, JWT, PIN_PEPPER, SESSION_MASTER_KEY)
```

## 5. Dominio + TLS (Let's Encrypt)

Elige dominio (comprado, o gratis con [duckdns.org](https://duckdns.org)) y apúntalo (A record) a
la IP pública. Luego:

```bash
# Instalar certbot y emitir el certificado (modo webroot sobre el puerto 80)
sudo apt-get install -y certbot
sudo certbot certonly --standalone -d tudominio.com \
  --deploy-hook "mkdir -p /home/ubuntu/CosTamarindo/infra/certs && \
    cp /etc/letsencrypt/live/tudominio.com/fullchain.pem /home/ubuntu/CosTamarindo/infra/certs/ && \
    cp /etc/letsencrypt/live/tudominio.com/privkey.pem /home/ubuntu/CosTamarindo/infra/certs/"

# Poner el dominio en la config de Nginx de producción
sed -i 's/__DOMAIN__/tudominio.com/g' infra/nginx-prod/costamarindo.prod.conf
```

## 6. Desplegar

```bash
cd /home/ubuntu/CosTamarindo
docker compose -f infra/docker-compose.yml -f infra/docker-compose.prod.yml \
  --env-file .env --profile backend up -d --build

# El primer arranque descarga qwen2.5:3b (1,9 GB). Verificar estado:
docker compose -f infra/docker-compose.yml -f infra/docker-compose.prod.yml --profile backend ps
curl https://tudominio.com/api/health
# -> {"status":"ok","env":"prod","database":true}
```

## 7. Verificación final

| Qué | Cómo | Esperado |
| :--- | :--- | :--- |
| Juego carga | abrir `https://tudominio.com` | `unityReady`, sin errores de consola |
| Leaderboard real | jugar, terminar, ver top-5 | datos desde Supabase PROD (tabla `scores`) |
| Reclamo top-5 | superar el top-5 y reclamar nombre + PIN | `rank_position` y perfil creado |
| NPC | abrir el bocadillo | respuesta de Ollama real por `/api/dialog` |
| Entornos separados (A.8.31) | arrancar con `ENV=prod` y `SUPABASE_PROJECT_REF` de DEV | el backend **se niega** a arrancar |
| E2E | `CT_E2E_BASE=https://tudominio.com backend\.venv\Scripts\python.exe backend/scripts/e2e_check.py` | `EXIT=0` |

## 8. Archivos que acompañan este runbook

| Archivo | Para qué |
| :--- | :--- |
| [`env.prod.example`](deploy/env.prod.example) | plantilla de variables de PROD (sin secretos) |
| [`../infra/docker-compose.prod.yml`](../infra/docker-compose.prod.yml) | override: TLS real, dominio y `ENV=prod` |
| [`../infra/nginx-prod/costamarindo.prod.conf`](../infra/nginx-prod/costamarindo.prod.conf) | Nginx de producción (Let's Encrypt, HTTP→HTTPS) |

## 9. Límites de lo gratuito (para que no sorprenda)

- **Supabase free:** 500 MB de base, 2 proyectos, ~50 000 usuarios de Auth/mes, ~5 GB de salida.
  Para una demo del juego es de sobra. El proyecto se pausa tras ~1 semana sin actividad.
- **Oracle Always Free:** 4 OCPU / 24 GB Arm + 200 GB de disco por cuenta. Suficiente para los 4
  contenedores. El tráfico de salida del Always Free tiene un límite mensual generoso (~10 TB),
  muy por encima del uso de un juego 2D con API ligera.
- **Ollama:** autocontenido y gratis; solo consume RAM/CPU de la VM.
- **Dominio:** único gasto opcional si quieres un dominio "bonito"; duckdns.org lo evita.

## 10. Qué falta validar en la VM (no se pudo probar aquí)

- El orden exacto de `certbot --standalone` (necesita el puerto 80 libre la primera vez; si el
  contenedor ya lo ocupa, emitir el certificado **antes** de `up -d`, o usar webroot).
- Los nombres/rutas de red de Oracle (VCN, security list, reserved IP) varían según la consola.
- La compilación WebGL dentro de la VM (si se decide compilar allí, instalar el módulo WebGL).
