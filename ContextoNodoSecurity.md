# Contexto — Nodo `Sg-Security` (CosTamarindo)

> **Documento para abrir un nuevo chat de OpenCode y empezar a trabajar.**
>
> **Ruta del proyecto:**
> `C:\Users\yoann\OneDrive\Desktop\universidad\Semestre 10\Seguridad Informatica\CosTamarindo`
>
> **Fecha del contexto:** 2026-11-13
>
> **Estado:** `Sg-Security` **cerrado operativamente** y con el primer bloque de evidencias
> independientes. La VM está `Running`, el agente `002` está **Active** y el manager recibe
> logs reales de Nginx. Tailscale queda para cuando estén los cinco nodos; `Sg-AI` ya está
> operativo con `qwen2.5:3b`, el backend conectado y el bocadillo probado en WebGL con un
> backend temporal; falta validarlo con Supabase real.
>
> **Alcance:** recuperar y operar la VM `sg-security`, integrarla como nodo `Sg-Security`
> del juego y dejar Wazuh, UFW, agente, reglas, logs y evidencias en un estado
> reproducible.
>
> **No contiene:** credenciales, tokens, claves ni secretos. Las IP internas del
> laboratorio (`172.18.x.x`, `172.19.x.x` y `10.8x.x.x`) se usan solo para operar el nodo.

> **Cierre 2026-11-13.** La VM `sg-security` quedó en `172.18.18.220` (el Default Switch
> cambió de `172.19.224.0/20` a `172.18.16.0/20`), con Wazuh 4.14.10 all-in-one, UFW
> `22/443/1514/1515` desde `172.18.16.0/20` y `172.19.224.0/20`, y `55000/9200` solo en
> localhost. El agente `sg-game-host` es el **ID 002** (el 001 fue purgado por `authd`) y
> está en el grupo `game`; se actualizó el manager sin volver a registrarlo. El frontend
> escribe `access.log` real; una ráfaga contra `/api/` generó `429` y `100250` disparó
> **36 alertas** en el manager. El FIM guarda estado en `002.db`, pero Wazuh 4.14 no emite
> la alerta por archivo en este all-in-one; se documenta como limitación. El backend se
> construyó, se reconstruyó como `latest` y **arranca healthy** desde el 2026-11-25 con las
> claves de `.env`; `backend.log` ya se escribe en `costamarindo-backend-logs`. La IP
> secundaria vieja `172.30.224.2/20` sigue en el host y requiere elevación para retirarla.
>
> **Bloque independiente 2026-11-13.** Se agregaron `evidencias/_plantilla/`,
> `evidencias/ataques/`, `docs/soa.md`, `evidencias/A.8.15/`, `evidencias/A.8.20/`,
> `evidencias/A.8.24/`, `evidencias/SEC-12/`, `evidencias/SEC-13/` y el workflow
> `.github/workflows/security.yml`. `pip-audit` pasó de 38 vulnerabilidades a 0 en runtime;
> los 53 tests del backend pasan. SonarQube sigue diferido.
>
> **Histórico:** la VM `sg-jacome-security` no se pudo recuperar y se reemplazó por
> `sg-security`, creada desde cero con `provision-security-node.sh`; la vieja se
> eliminó con `multipass delete --purge`. Las fases 0 y 1 describen el intento
> anterior; el procedimiento vigente está en `infra/wazuh/README.md`.

---

## 0. Cómo empezar en el nuevo chat

Abrir OpenCode en la ruta del proyecto y pedir algo como:

```text
Trabaja en C:\Users\yoann\OneDrive\Desktop\universidad\Semestre 10\Seguridad Informatica\CosTamarindo.
Lee ContextoNodoSecurity.md, Plan.md §7, docs/handoff/security-node.md y docs/handoff/security.md.
Objetivo: operar la VM sg-security y dejarla como nodo Sg-Security del juego.
No publiques puertos de la VM a Internet, no escribas secretos en el repo y no bloquees el arranque del juego si Wazuh falla.
```

Leer en este orden:

1. `ContextoNodoSecurity.md` — este documento.
2. `Plan.md` §7 — backlog de Security (`SEC-01`…`SEC-14`).
3. `docs/handoff/security-node.md` — procedimiento detallado de integración de la VM.
4. `docs/handoff/security.md` — recetas de ataque y estado por control.
5. `docs/arquitectura.md` — nodos, redes y flujo del entorno.
6. `infra/docker-compose.yml` — servicios, redes, perfiles y límites.

---

## 1. Proyecto y objetivo

CosTamarindo 2D es un videojuego Unity 6 (WebGL) con backend FastAPI, IA local con Ollama y infraestructura Docker. La división **Security** debe implementar, atacar, corregir y evidenciar los controles ISO/IEC 27001:2022 del Anexo A.

El nodo `Sg-Security` se cubre con la VM externa **`sg-security`** (Multipass, Ubuntu 24.04, Wazuh 4.14.10), reconstruida el 2026-11-10. La VM anterior `sg-jacome-security` quedó inaccesible y se descarta; la nueva se aprovisiona desde cero y conserva el aislamiento real respecto del equipo que corre el juego.

Regla de diseño: **la VM no está en la ruta de la partida**. Si Wazuh se cae, el juego sigue funcionando; la única pérdida es la evidencia en vivo.

---

## 2. Estado actual de la VM

> **Última verificación:** 2026-11-13. VM `Running` en `172.18.18.220`; SSH por clave y
> agente **002** activos. El Default Switch del host es `172.18.16.1/20`; la IP secundaria
> vieja `172.30.224.2/20` sigue en el adaptador y requiere elevación para retirarla.

| Elemento | Estado |
| :--- | :--- |
| Nombre real | `sg-security` (Multipass, hostname `sg-security`) |
| VM anterior | `sg-jacome-security` eliminada el 2026-11-10; solo queda `sg-security` |
| Origen | Reconstruida desde cero el 2026-11-10 con `infra/wazuh/provision-security-node.sh` |
| Hypervisor | Multipass sobre Hyper-V |
| Sistema operativo | Ubuntu 24.04.5 LTS |
| Recursos | 4 vCPU, 8 GB RAM, 30 GB disco |
| IP actual | `172.18.18.220` (Default Switch) |
| Wazuh | 4.14.10; manager + indexer + dashboard |
| Dashboard | Puerto 443 (solo Default Switch/tailnet) |
| Reglas/decoder | `infra/wazuh/rules/costamarindo_rules.xml` (100200–100250) |
| Estado de la VM | `Running`; manager, indexer, dashboard y agente operativos |
| Consola Hyper-V | No necesaria; el acceso operativo es SSH a `172.18.18.220` |
| Agente del host | `sg-game-host` (**ID 002**) **Active**, grupo `game`; ID 001 purgado por `authd` |
| Dashboard admin | Contraseña rotada; archivo fuera del repo (VM y host) |
| Tailscale de la VM | No instalado; se une a la tailnet nueva al final |
| UFW | 22/443/1514/1515 desde `172.18.16.0/20` y `172.19.224.0/20`; 55000/9200 en localhost |
| `infra/wazuh/` en el repo | **Existe**: reglas, grupo, README y `provision-security-node.sh` |
| Servicio `security` del compose | Perfil `security-local`; respaldo local, no se enciende con la VM |

### 2.1 Discrepancias a corregir

- La VM y el respaldo local deben usar la misma versión de Wazuh (**4.14.10**); el agente del host también.
- La VM publica el dashboard en **443**; `docs/arquitectura.md` ya usa 443. No volver a 5601.
- `docs/arquitectura.md` describe `Sg-Security` como contenedor en `mgmt`; la desviación a VM externa ya está documentada y la red `siem` tiene salida.

---

## 3. Decisiones cerradas

1. **Opción A:** usar la VM `sg-security` como nodo externo `Sg-Security`, no el servicio `security` del compose.
2. **Tailnet nueva:** crear una tailnet para el juego; la VM y el equipo del juego se unen con tags `game-security` y `game-host`.
3. **Tailscale solo como capa de administración y transporte:** no es un nodo del juego ni una VPN para jugadores.
4. **Nginx es el reverse proxy:** el 443 público es el de `Sg-Frontend`; la VM no publica el juego.
5. **Wazuh es SIEM/HIDS:** recibe logs, correlaciona reglas y hace FIM. **No** es un NIDS de red.
6. **No hay NIDS (Suricata/Snort) ni VPN** en el alcance actual.
7. **El agente inicia la conexión** hacia la VM por Tailscale en 1514/tcp y 1515/tcp.
8. **El dashboard** se abre solo por Tailscale o túnel SSH; nunca por la IP pública del equipo.
9. **`mgmt` sigue siendo `internal: true`;** el agente necesita además una red `siem` con salida.
10. **No encender la VM y el servicio `security` del compose al mismo tiempo.**
11. **No reutilizar literalmente las reglas 100100/100101 del taller:** los eventos del juego son otros.
12. **No bloquear el arranque del juego** si Wazuh no está disponible.

---

## 4. Arquitectura objetivo (solo Security)

```text
Jugador
  │ HTTPS 443 (único puerto público)
  ▼
┌──────────────────── Equipo del juego (Docker) ────────────────────┐
│  Sg-Frontend (Nginx)  ──/api/──▶  Sg-Backend  ──▶  Sg-AI          │
│       │                              │                            │
│       │ logs Nginx                   │ logs JSON                  │
│       └──────────┬───────────────────┘                            │
│                  ▼                                                │
│         sg-wazuh-agent (contenedor)                               │
└──────────────────┼────────────────────────────────────────────────┘
                   │ Tailscale (1514/tcp, 1515/tcp)
                   ▼
      ┌──────────────────────────────────────────┐
      │  VM sg-security                          │
      │  Wazuh manager + indexer                 │
      │  dashboard 443 (solo tailnet)            │
      │  reglas + FIM + evidencias               │
      └──────────────────────────────────────────┘
```

Reglas de red:

- El **443 público** es solo el de `Sg-Frontend`.
- La VM no publica nada a Internet.
- El agente sale hacia la VM por Tailscale.
- La VM no necesita entrar a las redes Docker: el agente inicia la conexión.
- El dashboard se abre por Tailscale o túnel SSH.

---

## 5. Plan de trabajo por fases

### Fase 0 — Recuperar la VM

> **Histórico:** esta fase describe el intento con `sg-jacome-security`. No se pudo
> recuperar la contraseña de `ubuntu` y se optó por reconstruir el nodo. El procedimiento
> vigente está en [`infra/wazuh/README.md`](infra/wazuh/README.md) (sección
> «Crear el nodo desde cero») y en `provision-security-node.sh`.

1. Abrir **Hyper-V Manager** como administrador o usar:

   ```powershell
   vmconnect.exe localhost "sg-jacome-security"
   ```

2. Iniciar sesión en la consola y comprobar:

   ```bash
   ip -br a
   systemctl status ssh --no-pager
   ufw status verbose
   sudo netplan get
   ```

3. Si no hay IP:

   ```bash
   sudo dhclient -v
   sudo systemctl restart systemd-networkd
   sudo netplan apply
   ```

   Si el netplan tiene una IP fija de la red vieja, cambiarla a `dhcp4: true` y aplicar.

4. Verificar Tailscale:

   ```bash
   sudo tailscale status
   ```

   Si sigue en la tailnet vieja, no hace falta arreglarla ahora: se cambia en la fase 1.

5. Último recurso, con confirmación (puede perder estado no escrito):

   ```powershell
   multipass stop --force sg-jacome-security
   multipass start sg-jacome-security
   ```

6. Plan B si la VM no revive: crear una VM nueva e instalar Wazuh all-in-one.

   ```powershell
   multipass launch 24.04 --name sg-security --cpus 4 --memory 8G --disk 40G
   ```

### Fase 1 — Tailnet nueva e identidad

1. Crear una tailnet nueva (o usar la del juego) y dos tags:
   - `tag:game-security` para la VM.
   - `tag:game-host` para el equipo que corre Docker.
2. Generar una auth key por tag. No pegarla en el repo ni en un chat; guardarla en un archivo temporal con permisos restringidos.
3. En la VM:

   ```bash
   sudo tailscale logout
   sudo tailscale up --auth-key=file:/root/ts-authkey \
     --hostname sg-security --ssh --accept-routes=false
   sudo tailscale status
   ```

4. En el equipo del juego:

   ```powershell
   Restart-Service Tailscale
   tailscale status
   tailscale up --auth-key=file:C:\ruta\ts-authkey --hostname game-host
   ```

5. ACL mínima de ejemplo (ajustar a la tailnet real):

   ```json
   {
     "tagOwners": {
       "tag:game-security": ["autogroup:admin"],
       "tag:game-host": ["autogroup:admin"]
     },
     "acls": [
       { "action": "accept", "src": ["tag:game-host"], "dst": ["tag:game-security:443,1514,1515"] },
       { "action": "accept", "src": ["autogroup:admin"], "dst": ["tag:game-security:22,443"] }
     ],
     "ssh": [
       { "action": "accept", "src": ["autogroup:admin"], "dst": ["tag:game-security"], "users": ["ubuntu"] }
     ]
   }
   ```

### Fase 2 — Hardening de la VM

```bash
sudo apt update && sudo apt upgrade -y
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow in on tailscale0 to any port 22 proto tcp
sudo ufw allow in on tailscale0 to any port 443 proto tcp
sudo ufw allow in on tailscale0 to any port 1514 proto tcp
sudo ufw allow in on tailscale0 to any port 1515 proto tcp
sudo ufw --force enable
sudo ufw status verbose
```

Comprobaciones:

- `iptables -L ts-input -n` y `iptables -L ts-forward -n`: Tailscale inserta reglas propias. Verificar que no abran más de lo que la ACL permite. En el taller hubo que neutralizar `ts-input`; repetir esa revisión.
- SSH: solo clave, sin password (`PasswordAuthentication no`) y `PermitRootLogin no`.
- Dashboard: cambiar la contraseña por defecto de `admin`, crear un usuario de solo lectura para la demo y no reutilizar credenciales del taller.
- Wazuh API (55000) y OpenSearch (9200) deben escuchar solo en `127.0.0.1`.
- No abrir 1514/1515/443 en la interfaz del Default Switch ni en la pública.

### Fase 3 — Wazuh: agentes, grupos y reglas

1. Confirmar que el manager acepta agentes:

   ```bash
   sudo grep -A3 '<remote>' /var/ossec/etc/ossec.conf
   sudo grep -A5 '<auth>' /var/ossec/etc/ossec.conf
   sudo /var/ossec/bin/wazuh-control status
   ```

   Debe haber `secure` en 1514/tcp y `authd` en 1515. Si se usa contraseña de registro:

   ```bash
   echo 'CAMBIAR_ESTA_CLAVE' | sudo tee /var/ossec/etc/authd.pass >/dev/null
   sudo chmod 600 /var/ossec/etc/authd.pass
   ```

   La contraseña va en `.env` del equipo del juego, nunca en el repo.

2. Crear el grupo `game` y su configuración centralizada:

   ```bash
   sudo mkdir -p /var/ossec/etc/shared/game
   sudo cp agent-group/game.conf /var/ossec/etc/shared/game/agent.conf
   sudo chown -R wazuh:wazuh /var/ossec/etc/shared/game
   ```

   Contenido de `infra/wazuh/agent-group/game.conf`:

   ```xml
   <agent_config>
     <localfile>
       <log_format>json</log_format>
       <location>/var/log/costamarindo/backend.log</location>
     </localfile>
     <localfile>
       <log_format>apache</log_format>
       <location>/var/log/nginx/access.log</location>
     </localfile>
     <syscheck>
       <directories check_all="yes" report_changes="yes" realtime="yes">/fim/webgl</directories>
     </syscheck>
   </agent_config>
   ```

   `realtime` es útil para la demo; si el consumo de inotify molesta, cambiar a `frequency="300"`.

3. Copiar reglas y decoders al manager (mantener la copia fuente en el repo):

   ```bash
   sudo cp infra/wazuh/rules/costamarindo_rules.xml /var/ossec/etc/rules/
   sudo chown wazuh:wazuh /var/ossec/etc/rules/costamarindo_rules.xml
   sudo /var/ossec/bin/wazuh-control restart
   ```

   > **Borrador histórico (no usar).** Las reglas vigentes están en
   > [`infra/wazuh/rules/costamarindo_rules.xml`](infra/wazuh/rules/costamarindo_rules.xml)
   > con IDs **100200–100250**. El bloque siguiente se conserva solo como registro y
   > **no debe copiarse**; no se reutilizan las 100100/100101 del taller.

   ```xml
   <group name="costamarindo,">
     <rule id="100100" level="7">
       <decoded_as>json</decoded_as>
       <field name="event">^score_rejected$</field>
       <description>CosTamarindo: puntaje rechazado (reason=$(reason))</description>
     </rule>

     <rule id="100101" level="10" frequency="5" timeframe="120">
       <if_matched_sid>100100</if_matched_sid>
       <same_field>user_id</same_field>
       <description>CosTamarindo: 5 rechazos de puntaje en 120 s para el mismo usuario</description>
     </rule>

     <rule id="100110" level="8">
       <decoded_as>json</decoded_as>
       <field name="event">^login_fail$</field>
       <description>CosTamarindo: login fallido</description>
     </rule>

     <rule id="100111" level="10" frequency="5" timeframe="120">
       <if_matched_sid>100110</if_matched_sid>
       <same_field>username</same_field>
       <description>CosTamarindo: 5 logins fallidos en 120 s para el mismo usuario</description>
     </rule>

     <rule id="100120" level="9">
       <decoded_as>json</decoded_as>
       <field name="event">^dialog_blocked$</field>
       <description>CosTamarindo: prompt injection bloqueado</description>
     </rule>

     <rule id="100130" level="10">
       <if_sid>550</if_sid>
       <match>/fim/webgl</match>
       <description>CosTamarindo: cambio en el webroot (FIM)</description>
     </rule>
   </group>
   ```

   Notas:

   - El backend ya emite JSON con `event`, `request_id`, `user_id`, `reason` e `ip` (`backend/app/logging_setup.py`). No hace falta decoder propio: Wazuh usa el decoder `json` integrado.
   - La IP se enmascara (último octeto a `0`), así que **no** conviene correlacionar por IP. Usar `user_id` o `request_id`. Si la versión de Wazuh no soporta `same_field`, correlacionar por `username` o dejar la regla de frecuencia sin campo.
   - `100130` se apoya en las reglas de FIM de Wazuh (550/553/554). Verificar el ID exacto con `wazuh-logtest` en la versión instalada.

4. Validar sin esperar al juego:

   ```bash
   sudo /var/ossec/bin/wazuh-logtest
   ```

   Pegar una línea como:

   ```json
   {"ts":"2026-11-08T10:00:00.000Z","level":"INFO","logger":"costamarindo.audit","event":"score_rejected","request_id":"test-1","user_id":"00000000-0000-0000-0000-000000000000","reason":"bad_signature","ip":"203.0.113.0"}
   ```

   Debe salir el decoder `json` y la regla vigente `100200` (el `100100` del borrador
   histórico no se usa).

### Fase 4 — Integración con el compose del juego

Cambios mínimos, todos detrás del perfil `security` para no afectar el arranque normal:

1. **Red `siem`** (con salida; `mgmt` sigue siendo `internal` para logs entre contenedores):

   ```yaml
   networks:
     siem:
       name: costamarindo-siem
   ```

2. **Volúmenes de logs**:

   ```yaml
   volumes:
     frontend-logs:
       name: costamarindo-frontend-logs
     backend-logs:
       name: costamarindo-backend-logs
   ```

   En `frontend`, añadir:

   ```yaml
   volumes:
     - frontend-logs:/var/log/nginx
   ```

   En `backend`, añadir:

   ```yaml
   volumes:
     - backend-logs:/var/log/costamarindo
   ```

   El backend hoy escribe solo a stdout. Para que el agente lea un archivo, añadir un `FileHandler` a `/var/log/costamarindo/backend.log` cuando `LOG_DIR` esté definido, y crear el directorio con permisos del usuario `app` en el `Dockerfile`:

   ```dockerfile
   RUN mkdir -p /var/log/costamarindo && chown app:app /var/log/costamarindo
   ```

   Es un cambio pequeño de Backend (BE-12 o dependencia de SEC-08). Sin él, el agente solo puede leer logs de Nginx.

3. **Agente Wazuh** (perfil `security`):

   ```yaml
   wazuh-agent:
     image: wazuh/wazuh-agent:4.14.10
     container_name: sg-wazuh-agent
     profiles: ["security"]
     restart: unless-stopped
     environment:
       WAZUH_MANAGER: "${WAZUH_MANAGER:?define la IP de Tailscale de la VM}"
       WAZUH_AGENT_NAME: "sg-game-host"
       WAZUH_REGISTRATION_SERVER: "${WAZUH_MANAGER}"
       WAZUH_AGENT_GROUP: "game"
       WAZUH_REGISTRATION_PASSWORD: "${WAZUH_REGISTRATION_PASSWORD:?define la clave de authd}"
     volumes:
       - ../builds/webgl:/fim/webgl:ro
       - frontend-logs:/var/log/nginx:ro
       - backend-logs:/var/log/costamarindo:ro
     networks:
       - mgmt
       - siem
     mem_limit: 256m
   ```

   `mgmt` es `internal: true`; por eso el agente necesita además `siem` para alcanzar la VM por Tailscale. Si Docker Desktop no enruta al rango de Tailscale, probar primero con un contenedor temporal; si falla, alternativas: sidecar `tailscale/tailscale` con el agente en su namespace, o usar la IP del Default Switch con UFW restringido a esa subred. Tailscale es preferible porque la IP no cambia.

4. **`.env.example`** (sin valores reales):

   ```dotenv
   WAZUH_MANAGER=100.x.y.z
   WAZUH_REGISTRATION_PASSWORD=cambiar
   ```

5. **No levantar los dos SIEM a la vez.** Si se usa la VM, el servicio `security` del compose queda como respaldo documentado, no como algo que se enciende en la demo.

### Fase 5 — Pruebas sin el juego terminado

| Prueba | Cómo | Resultado esperado |
| :--- | :--- | :--- |
| Enrollment | `docker exec -it sg-wazuh-agent /var/ossec/bin/wazuh-control status` | agente `active` |
| Evento sintético | escribir una línea JSON de `score_rejected` en el volumen de logs | alerta 100200 en el dashboard |
| Frecuencia | 5 líneas `login_fail` con el mismo `username` en < 120 s | alerta 100211 |
| Prompt injection | línea `dialog_blocked` | alerta 100221 |
| FIM | crear y borrar `builds/webgl/.fim-test` | estado en `002.db`; sin alerta por archivo (limitación 4.14) |
| Aislamiento | `nmap -Pn -p 1514,1515,443 <IP-Tailscale-VM>` desde fuera de la tailnet | `filtered` |
| Dashboard | abrir `https://<IP-Tailscale-VM>` solo desde la tailnet | login, sin exposición pública |

### Fase 6 — Pruebas con el juego y evidencias

Cuando el juego esté listo:

1. Ejecutar las recetas de [`security.md`](docs/handoff/security.md) §3 (Burp, replay, fuerza bruta, SQLi, XSS, prompt injection, `nmap`).
2. Para cada control, guardar en `evidencias/<control>/`:
   - salida literal del ataque,
   - alerta correlacionada de Wazuh (JSON/CSV exportado, sin secretos),
   - captura del dashboard,
   - corrección aplicada y verificación.
3. Controles que este nodo puede evidenciar directamente:

| Control | Qué aporta el nodo | Evidencia |
| :--- | :--- | :--- |
| A.8.24 Criptografía | logs `score_rejected` por `bad_signature`/`session_used` | ataque + alerta 100200/100201 |
| A.8.5 Autenticación | `login_fail`/`login_blocked` | fuerza bruta + alerta 100210/100211/100212 |
| A.8.9 Configuración | servidor autoritativo; rechazo de puntaje alterado | edición de IndexedDB + log |
| A.8.20 Redes | `nmap` y aislamiento Docker | salida literal + dashboard |
| A.8.12 DLP | sin secretos en consola/`strings` | captura + `strings` |
| A.8.15 Logging | reglas, correlación y dashboard | alertas + `wazuh-logtest` |
| A.8.28 Codificación | `dialog_blocked`, `register_rejected` | SQLi/XSS/injection + alerta |
| A.8.25 SDLC | Gitleaks (hook/CI), no Wazuh | salida de Gitleaks |
| A.8.19 Instalación | FIM del webroot + `SHA256SUMS`/GPG | cambio de `.wasm` + alerta |

El backlog completo y los controles restantes están en `Plan.md` §7.

### Fase 7 — Operación y rollback

- **Operación:** el agente y la VM se levantan aparte del juego. Si Wazuh falla, el juego sigue; la única pérdida es la evidencia en vivo.
- **Rollback:** `docker compose --profile security stop wazuh-agent`, apagar la VM o restaurar un checkpoint de Hyper-V. El juego no se toca.
- **Actualizaciones:** no actualizar Wazuh de versión mayor sin checkpoint y sin repetir `wazuh-logtest`; las reglas y el dashboard pueden cambiar.
- **Secretos:** auth key de Tailscale, contraseña de `authd` y `admin` del dashboard fuera del repo; `.env.example` solo con placeholders.

---

## 6. Qué está listo y qué falta

### 6.1 Listo

- VM `sg-security` (Ubuntu 24.04.5, Wazuh 4.14.10 all-in-one) en `Running`, IP
  `172.18.18.220`; SSH por clave y agente **002** activos. UFW restringe
  22/443/1514/1515 a `172.18.16.0/20` y `172.19.224.0/20`; API 55000 e índice 9200
  solo en `127.0.0.1`.
- Reglas `100200–100250` cargadas. `100200` (7), `100210` (5) y `100250` (36)
  verificadas con eventos reales; `100240/100241` cargan sobre 550/553/554.
- Agente `sg-game-host` (**ID 002**) **Active**, grupo `game`, con FIM de `/fim/webgl`
  y logs de Nginx/backend configurados. Se actualizó el manager a `172.18.18.220`
  sin volver a registrar el agente.
- Logs reales de Nginx: `access.log` es archivo regular en `frontend-logs`, el agente
  lo lee y `100250` disparó **36 veces** con una ráfaga `429`.
- Dashboard en `443`; contraseña de `admin` rotada. Authd y credenciales fuera del
  repo (`.env` ignorado por git y `C:\Users\yoann\.config\costamarindo\`).
- El backend emite logs JSON pensados para Wazuh (`backend/app/logging_setup.py`);
  la imagen se construyó y las dependencias se actualizaron, pero el contenedor no
  arranca sin las claves del `.env`.
- El compose declara los servicios, el agente detrás de `security` y el respaldo
  `security-local`; `infra/scripts/up.ps1` y `up.sh` cargan el `.env` de la raíz.
- Documentación de integración en `docs/handoff/security-node.md`,
  `infra/wazuh/README.md` y `docs/arquitectura.md`.
- A.8.25 (Gitleaks) implementado y evidenciado.
- A.8.20/A.8.24/A.8.15 con evidencia parcial en `evidencias/`; `docs/soa.md` con
  93 controles evaluados y matriz de riesgos; `SEC-12` con `pip-audit` en 0 y
  `SEC-13` con workflow de CI.

### 6.2 Falta

- Confirmar la alerta FIM end-to-end: Wazuh 4.14 guarda el `fim_entry` pero no emite
  la alerta por archivo en este all-in-one; la limitación ya está documentada en
  `evidencias/A.8.15/README.md` y no se afirma que `100240/100241` dispararon.
- Crear la tailnet nueva, tags/ACL y unir la VM; ajustar UFW para `tailscale0`.
- ~~Cargar las claves de Supabase/JWT y levantar el backend para generar `backend.log`~~
  **Hecho el 2026-11-25:** backend healthy y `backend.log` escribiéndose; ahora toca producir
  las evidencias de ataque con el backend real.
- Completar A.8.20 con `nmap` desde un contenedor de la DMZ.
- Completar A.8.24 con cifrado en reposo y gestión de claves.
- SonarQube, Bearer y las pruebas de F9/F8 al final.


### 6.3 Backlog de Security (resumen)

| ID | Tarea | Estado |
| :--- | :--- | :--- |
| `SEC-01` | Plantilla de evidencia y scripts en `evidencias/ataques/` | ✅ |
| `SEC-02` | `docs/soa.md` + matriz de riesgos | ✅ 93 controles |
| `SEC-03` | A.8.24: Burp + replay + TLS | API lista; TLS/headers ya evidenciados |
| `SEC-04` | A.8.5: fuerza bruta + JWT | API lista |
| `SEC-05` | A.8.9: IndexedDB | depende de `FE-07` |
| `SEC-06` | A.8.20: `nmap` host y contenedor | 🟡 host+VM listos; falta contenedor |
| `SEC-07` | A.8.12: consola + `strings` | depende de `FE-12` |
| `SEC-08` | A.8.15: Wazuh + reglas + FIM | 🟡 Nginx real, `100250` (36) y `backend.log`; falta alerta FIM end-to-end y tailnet |
| `SEC-09` | A.8.28: SQLi, XSS, prompt injection | API lista |
| `SEC-11` | A.8.19: `SHA256SUMS` + GPG + FIM | depende de `FE-12` |
| `SEC-12` | SonarQube + `pip-audit`/`npm audit` | 🟡 `pip-audit` en 0; SonarQube diferido |
| `SEC-13` | CI en GitHub Actions (Gitleaks + tests) | ✅ workflow; falta primera ejecución remota |
| `SEC-14` | Quitar `com.coplaydev.unity-mcp` del `manifest.json` | coordinar con Frontend |

### 6.4 Bloqueos actuales

- La IP secundaria vieja `172.30.224.2/20` sigue en el host; retirarla requiere una
  consola elevada. La red actual es `172.18.16.0/20` y se conserva la regla anterior
  `172.19.224.0/20`.
- El FIM de Wazuh 4.14 no emite alerta por archivo en este all-in-one; solo se
  confirma el estado en `002.db`. Es una limitación de la versión, no de la regla.
- ~~El backend no arranca porque faltan las claves de Supabase/JWT en el `.env` real.~~
  **Resuelto el 2026-11-25:** backend healthy y `backend.log` disponible.
- Tailscale se difiere hasta tener los cinco nodos; `Sg-AI` está healthy y el endpoint de
  diálogo ya llama a Ollama. El bocadillo ya está probado en WebGL con backend temporal;
  falta validarlo con el backend real de Supabase.
- `multipass info/exec` falla por DNS/KVP (`sg-security.mshome.net` apunta a la IP
  vieja); el acceso operativo es SSH directo a `172.18.18.220`.
- `builds/webgl` ya está generado y `https://juego.local/` responde `200`; el arte sigue
  pendiente de `FE-02`. La demo completa ya se puede hacer con el build actual.

---

## 7. Archivos clave

| Archivo | Para qué |
| :--- | :--- |
| `Plan.md` §7 | Backlog de Security y criterio de terminado |
| `docs/handoff/security-node.md` | Procedimiento detallado de integración de la VM |
| `docs/handoff/security.md` | Recetas de ataque y estado por control |
| `docs/arquitectura.md` | Nodos, redes, compose y flujo del entorno |
| `infra/docker-compose.yml` | Servicios, redes, perfiles y límites |
| `backend/app/logging_setup.py` | Formato JSON de eventos para Wazuh |
| `.env.example` | Nombres de variables; nunca valores reales |
| `infra/scripts/tls.ps1` | Certificado local de `juego.local` |
| `infra/scripts/up.ps1` | Arranque del entorno por perfil |
| `evidencias/` | Una carpeta por control ISO |
| `infra/wazuh/` | **A crear:** reglas, grupo de agentes y README |
| `ContextoNodoSecurity.md` | Este documento |

---

## 8. Comandos útiles

### Multipass / Hyper-V

```powershell
multipass list
multipass info sg-security
multipass shell sg-security
multipass exec sg-security -- <comando>
```

### Dentro de la VM

```bash
ip -br a
systemctl status ssh --no-pager
ufw status verbose
sudo netplan get
sudo tailscale status
sudo tailscale ip -4
sudo /var/ossec/bin/wazuh-control status
sudo ss -tlnp
```

### Tailscale en el host

```powershell
Restart-Service Tailscale
tailscale status
tailscale ping sg-security
Test-NetConnection <IP-Tailscale-VM> -Port 22
```

### Wazuh

```bash
sudo grep -A3 '<remote>' /var/ossec/etc/ossec.conf
sudo grep -A5 '<auth>' /var/ossec/etc/ossec.conf
sudo /var/ossec/bin/wazuh-control status
sudo /var/ossec/bin/wazuh-logtest
```

### Docker del juego

```powershell
cd infra
docker compose ps
docker compose --profile security up -d
docker compose --profile security logs -f wazuh-agent
docker compose --profile security stop wazuh-agent
```

---

## 9. Reglas y cuidados

1. **No publicar** 1514/1515/443 de la VM a Internet.
2. **No usar** el 443 de la VM como entrada del juego: el único 443 público es `Sg-Frontend`.
3. **No levantar** la VM y el servicio `security` del compose al mismo tiempo.
4. **No meter** la auth key de Tailscale ni contraseñas en `infra/`, `.env.example` ni docs.
5. **No reutilizar** literalmente las reglas 100100/100101 del taller: los eventos del juego son `score_rejected`, `login_fail`, `login_blocked` y `dialog_blocked`.
6. **No bloquear** el arranque del juego si Wazuh no está disponible: el nodo es de observación.
7. **La evidencia se produce ejecutando el ataque**, no describiéndolo.
8. **No usar** `git commit --no-verify`.
9. **Enmascarar IPs reales** en capturas y logs; el backend ya enmascara el último octeto.
10. **No actualizar Wazuh de versión mayor** sin checkpoint y sin repetir `wazuh-logtest`.

---

## 10. Primeros pasos recomendados en el nuevo chat

1. Ejecutar `git status` y leer los archivos clave.
2. Recuperar la VM por consola Hyper-V y verificar red/SSH/UFW.
3. Crear `infra/wazuh/` con `rules/`, `agent-group/` y `README.md`.
4. Añadir los volúmenes de logs y el `FileHandler` del backend.
5. Añadir el servicio `wazuh-agent` al compose, detrás del perfil `security`.
6. Probar reglas y FIM con eventos sintéticos.
7. Actualizar `docs/arquitectura.md` y `Plan.md` §9.1 con la desviación de la VM externa.
8. Cuando el juego esté listo, ejecutar las recetas de ataque y producir las evidencias.

---

## 11. Decisiones abiertas

- ~~¿Se recupera la VM actual o se crea una nueva?~~ **Resuelto:** VM nueva `sg-security`; la vieja se elimina al validar la nueva.
- Nombre y propietario de la tailnet del juego.
- ~~Puerto del dashboard~~ **Resuelto:** 443 (solo Default Switch/tailnet).
- ~~Versión de Wazuh~~ **Resuelto:** 4.14.10 en la VM, el respaldo local y el agente.
- ¿Tailscale sidecar para el agente o ruta del host hacia la VM?
- ¿La demo usa solo la VM o deja el servicio `security` del compose como respaldo?
- ¿Qué control se evidencia primero después de A.8.25?

---

## 12. Prompt sugerido para el nuevo chat

```text
Trabaja en C:\Users\yoann\OneDrive\Desktop\universidad\Semestre 10\Seguridad Informatica\CosTamarindo.

Lee ContextoNodoSecurity.md, Plan.md §7, docs/handoff/security-node.md y docs/handoff/security.md.

Objetivo: operar el nodo Sg-Security en la VM sg-security (Ubuntu 24.04, Wazuh 4.14.10) y dejarlo con UFW, agente, reglas y FIM funcionando. La VM vieja sg-jacome-security se descarta.

Restricciones:
- No publiques 1514/1515/443 de la VM a Internet.
- No escribas secretos en el repo ni en la documentación.
- No bloquees el arranque del juego si Wazuh falla.
- No reutilices literalmente las reglas del taller.
- Antes de cambiar algo destructivo, pide confirmación.
```
