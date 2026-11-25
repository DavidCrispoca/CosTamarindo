# Nodo `Sg-Security` — procedimiento de integración

> **Cierre operativo 2026-11-13:** la VM vigente es **`sg-security`** en
> `172.18.18.220`, con Wazuh 4.14.10 all-in-one y UFW `22/443/1514/1515` desde
> `172.18.16.0/20` y `172.19.224.0/20`. El agente `sg-game-host` es el **ID 002**
> (grupo `game`) y está **Active**. Los logs reales de Nginx llegan al manager y
> `100250` disparó 36 veces con una ráfaga `429`. El FIM detecta el cambio y lo guarda
> en `002.db`, pero Wazuh 4.14 no emite la alerta por archivo en este all-in-one; es una
> limitación documentada, no un fallo de la regla. `backend.log` ya se escribe desde el
> 2026-11-25 en el volumen `costamarindo-backend-logs`. Tailscale se difiere hasta tener los
> cinco nodos.
>
> **Reglas reales:** [`infra/wazuh/rules/costamarindo_rules.xml`](../../infra/wazuh/rules/costamarindo_rules.xml)
> con IDs **100200–100250**; no se reutilizan las 100100/100101 del taller. La
> operación vigente está en [`infra/wazuh/README.md`](../../infra/wazuh/README.md).
>
> **Estado de este documento:** la propuesta de integración de 2026-11-08 y las
> secciones marcadas como históricas se conservan como registro. La VM anterior
> `sg-jacome-security` quedó inaccesible y se eliminó; la actual se creó desde cero con
> [`infra/wazuh/provision-security-node.sh`](../../infra/wazuh/provision-security-node.sh).
> **Fuente de verdad:** [`../../Plan.md`](../../Plan.md) §7 y §9.1.

---

## 1. Decisión propuesta

Usar la VM `sg-security` como **nodo externo `Sg-Security`** del juego, en lugar de
levantar el servicio `security` del compose. La VM conserva Wazuh, el dashboard y el
aislamiento real; el juego sigue corriendo en Docker en el equipo anfitrión. La sección
histórica menciona `sg-jacome-security`, que fue reemplazada.

La VM **no** entra en la ruta de la partida: si Wazuh se cae, el juego sigue funcionando.
Eso permite integrarla al final (F8) sin bloquear F5/F6/F7.

### 1.1 Lo que dice el plan (y lo que no)

| Pieza | ¿Está en el plan? | Dónde / cómo |
| :--- | :--- | :--- |
| **Reverse proxy** | **Sí** | Nginx de `Sg-Frontend` publica 443, termina TLS y proxifica `/api/` a `sg-backend:8000`. No es un nodo aparte. |
| **IDS/SIEM** | **Sí, como Wazuh** | `Sg-Security`: manager + indexer + dashboard, agentes, reglas `100200–100250` y FIM. Wazuh es HIDS/SIEM, **no** un NIDS de red. |
| **NIDS (Suricata/Snort)** | **No** | No aparece en `Plan.md`, `docs/arquitectura.md` ni `docs/CosTamarindo.md`. Sería alcance extra. |
| **VPN** | **No** | El juego no usa VPN: el jugador entra por HTTPS 443. |
| **Tailscale** | **No** | No aparece en el plan del juego. Solo se usó en el taller para conectar nodos. Para el juego sería una **capa de administración**, no un nodo. |

**Conclusión:** no hace falta añadir un nodo de VPN ni un reverse proxy nuevo. El reverse
proxy ya es Nginx y el IDS/SIEM es Wazuh. Tailscale se justifica solo para administrar la VM
y transportar los eventos de los agentes sin abrir puertos a Internet.

---

## 2. Estado actual (2026-11-13)

| Elemento | Estado |
| :--- | :--- |
| VM `sg-security` | `Running`, IP `172.18.18.220`, Ubuntu 24.04.5, 4 vCPU / 8 GB / 30 GB. |
| Wazuh | 4.14.10 all-in-one; manager, indexer y dashboard en 443. |
| Agente | `sg-game-host`, **ID 002**, grupo `game`, **Active**. |
| UFW | `22/443/1514/1515` desde `172.18.16.0/20` y `172.19.224.0/20`; `55000/9200` solo localhost. |
| Reglas | `100200–100250` cargadas; `100200/100210/100250` verificadas con eventos reales. |
| Logs Nginx | `access.log` real en el volumen; el agente lo lee; `100250` disparó 36 veces. |
| FIM | Estado en `002.db` (`changes=1`), sin alerta por archivo en Wazuh 4.14 all-in-one. |
| Backend | `costamarindo-backend:latest` healthy con las claves en `.env`; `backend.log` escribiéndose en `backend-logs` (2026-11-25). |
| Tailscale | Diferido hasta tener los cinco nodos. |
| `infra/wazuh/` | Existe con reglas, grupo, README y `provision-security-node.sh`. |
| Evidencias | `A.8.15`, `A.8.20`, `A.8.24`, `SEC-12`, `SEC-13` y `docs/soa.md`. |

El procedimiento de la §4.1 y las secciones marcadas como históricas se conservan como
registro; la operación vigente está en [`infra/wazuh/README.md`](../../infra/wazuh/README.md).

---

## 3. Opciones

| Criterio | **A. VM externa (recomendada)** | **B. Servicio del compose** |
| :--- | :--- | :--- |
| Fidelidad a `Plan.md` | Requiere documentar la desviación | Alta: ya está declarado |
| Aislamiento | Real: otro host, otra red, Tailscale/ACL | Mismo host Docker |
| RAM en el equipo del juego | 0 GB (la VM corre aparte) | ~3 GB |
| Estado del taller | Se conserva Wazuh y sus reglas | Se pierde si no se migra |
| Complejidad | Agente, red `siem`, Tailscale | Un `--profile security` |
| Riesgo si falla | El juego no se afecta | El juego no se afecta |
| Recomendación | **Sí**, si la VM se recupera | Plan B si la VM no revive |

La opción A es la que mejor cuenta la historia ISO: nodo separado, segmentación, ACL y
evidencias en un SIEM distinto del host del juego. La opción B queda como respaldo y para
probar reglas rápido en local.

---

## 4. Arquitectura objetivo (opción A)

```
Jugador
  │ HTTPS 443 (único puerto público)
  ▼
┌────────────────────────── Equipo del juego (Docker) ──────────────────────────┐
│  Sg-Frontend (Nginx)  ──/api/──▶  Sg-Backend (FastAPI)  ──▶  Supabase Cloud    │
│       │                              │                                        │
│       │ logs Nginx                   │ logs JSON                              │
│       └──────────┬───────────────────┘                                        │
│                  ▼                                                            │
│         sg-wazuh-agent (contenedor)  ── Tailscale (1514/tcp) ──┐              │
└─────────────────────────────────────────────────────────────────┼──────────────┘
                                                                  │
                                              ┌───────────────────▼──────────────┐
                                              │  VM sg-jacome-security           │
                                              │  Wazuh manager + indexer         │
                                              │  dashboard 443 (solo tailnet)    │
                                              │  reglas + FIM + evidencias       │
                                              └──────────────────────────────────┘
```

Reglas de red:

- El **443 público** es solo el de `Sg-Frontend`. La VM no publica nada a Internet.
- El agente sale hacia la VM por Tailscale (`100.x.y.z:1514` y `:1515`).
- El dashboard se abre por Tailscale (`https://100.x.y.z`) o por túnel SSH; nunca por la IP
  pública del equipo.
- La VM no necesita entrar a las redes Docker: el agente es quien inicia la conexión.

> **Desviación documentada:** `docs/arquitectura.md` y `Plan.md` describen `Sg-Security`
> como servicio del compose en la red `mgmt` (`internal: true`). Al usar una VM externa, el
> agente necesita una red con salida (`siem`) y la VM deja de ser un contenedor. Hay que
> actualizar §2 y §3 de `docs/arquitectura.md` en el mismo PR de la integración.

---

## 5. Procedimiento

### 5.1 Fase 0 — Recuperar la VM

1. Abrir **Hyper-V Manager** como administrador (`Win+X` → Terminal (admin) →
   `virtmgmt.msc`) o `vmconnect.exe localhost "sg-jacome-security"`.
2. Iniciar sesión en la consola y comprobar red y SSH:

   ```bash
   ip -br a
   systemctl status ssh --no-pager
   ufw status verbose
   sudo netplan get
   ```

3. Si no hay IP, pedir DHCP y aplicar netplan:

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

5. Si la consola no responde o el arranque está colgado, el último recurso es apagar a la
   fuerza y volver a arrancar (requiere confirmación; puede perder el estado no escrito):

   ```powershell
   multipass stop --force sg-jacome-security
   multipass start sg-jacome-security
   ```

   Si aun así no vuelve, **plan B**: crear una VM nueva (`multipass launch 24.04 --name
   sg-security --cpus 4 --memory 8G --disk 40G`) e instalar Wazuh all-in-one. El taller ya
   está cerrado; no se pierde evidencia del juego porque todavía no existe.

### 5.2 Fase 1 — Tailnet nueva e identidad

1. En la consola de Tailscale, crear una **tailnet nueva** (o usar la que ya se vaya a
   usar para el juego) y dos tags:
   - `tag:game-security` para la VM.
   - `tag:game-host` para el equipo que corre Docker.
2. Generar una auth key para cada tag. No pegarla en el repo ni en un chat; guardarla en un
   archivo temporal con permisos restringidos.
3. En la VM:

   ```bash
   sudo tailscale logout
   sudo tailscale up --auth-key=file:/root/ts-authkey \
     --hostname sg-security --ssh --accept-routes=false
   sudo tailscale status
   ```

   `--ssh` permite entrar por Tailscale SSH y evita depender de la IP del Default Switch.
   Si no se quiere Tailscale SSH, dejar solo `--hostname` y mantener SSH por clave.
4. En el equipo del juego (Windows), arreglar el cliente y unirlo a la misma tailnet:

   ```powershell
   Restart-Service Tailscale   # requiere elevación
   tailscale status
   tailscale up --auth-key=file:C:\ruta\ts-authkey --hostname game-host
   ```

5. Verificar desde el host:

   ```powershell
   tailscale ping sg-security
   Test-NetConnection 100.x.y.z -Port 22
   ```

6. ACL mínima (ejemplo; ajustar a la tailnet real):

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

   El host solo necesita 443/1514/1515; el administrador necesita 22/443. Todo lo demás
   queda denegado por defecto.

### 5.3 Fase 2 — Hardening de la VM

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

- `iptables -L ts-input -n` y `iptables -L ts-forward -n`: Tailscale inserta reglas propias.
  Verificar que no abran más de lo que la ACL permite. En el taller hubo que neutralizar
  `ts-input`; repetir esa revisión.
- SSH: solo clave, sin password (`PasswordAuthentication no`), y `PermitRootLogin no`.
- Dashboard: cambiar la contraseña por defecto de `admin`, crear un usuario de solo lectura
  para la demo y no reutilizar credenciales del taller.
- Wazuh API (55000) y OpenSearch (9200) deben escuchar solo en `127.0.0.1`.
- No abrir 1514/1515/443 en la interfaz del Default Switch ni en la pública.

### 5.4 Fase 3 — Wazuh: agentes, grupos y reglas

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

   `realtime` es útil para la demo; si el consumo de inotify molesta, cambiar a
   `frequency="300"`.

3. Copiar reglas y decoders al manager (mantener la copia fuente en el repo):

   ```bash
   sudo cp infra/wazuh/rules/costamarindo_rules.xml /var/ossec/etc/rules/
   sudo chown wazuh:wazuh /var/ossec/etc/rules/costamarindo_rules.xml
   sudo /var/ossec/bin/wazuh-control restart
   ```

   > **Borrador histórico (no usar).** Las reglas vigentes están en
   > [`infra/wazuh/rules/costamarindo_rules.xml`](../../infra/wazuh/rules/costamarindo_rules.xml)
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
   - El backend ya emite JSON con `event`, `request_id`, `user_id`, `reason` e `ip`
     (`backend/app/logging_setup.py`). No hace falta decoder propio: Wazuh usa el decoder
     `json` integrado.
   - La IP se enmascara (último octeto a `0`), así que **no** conviene correlacionar por
     IP. Usar `user_id` o `request_id`. Si la versión de Wazuh no soporta `same_field`,
     correlacionar por `username` o dejar la regla de frecuencia sin campo.
   - `100130` se apoya en las reglas de FIM de Wazuh (550/553/554). Verificar el ID exacto
     con `wazuh-logtest` en la versión instalada.

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

### 5.5 Fase 4 — Integración con el compose del juego

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

   El backend hoy escribe solo a stdout. Para que el agente lea un archivo, añadir un
   `FileHandler` a `/var/log/costamarindo/backend.log` cuando `LOG_DIR` esté definido, y
   crear el directorio con permisos del usuario `app` en el `Dockerfile`:

   ```dockerfile
   RUN mkdir -p /var/log/costamarindo && chown app:app /var/log/costamarindo
   ```

   Es un cambio pequeño de Backend (BE-12 o dependencia de SEC-08). Sin él, el agente solo
   puede leer logs de Nginx.

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

   `mgmt` es `internal: true`; por eso el agente necesita además `siem` para alcanzar la VM
   por Tailscale. Si Docker Desktop no enruta al `100.x`, probar primero:

   ```powershell
   docker run --rm curlimages/curl -k -sS https://100.x.y.z/ -o NUL
   ```

   Si falla, alternativas: sidecar `tailscale/tailscale` con el agente en su namespace, o
   usar la IP del Default Switch (`172.24.x.x`) con UFW restringido a `172.24.96.0/20`.
   Tailscale es preferible porque la IP no cambia.

4. **`.env.example`** (sin valores reales):

   ```dotenv
   WAZUH_MANAGER=100.x.y.z
   WAZUH_REGISTRATION_PASSWORD=cambiar
   ```

5. **No levantar los dos SIEM a la vez.** Si se usa la VM, el servicio `security` del
   compose queda como respaldo documentado, no como algo que se enciende en la demo.

### 5.6 Fase 5 — Pruebas sin el juego terminado

| Prueba | Cómo | Resultado esperado |
| :--- | :--- | :--- |
| Enrollment | `docker exec -it sg-wazuh-agent /var/ossec/bin/wazuh-control status` | agente `active` |
| Evento sintético | escribir una línea JSON de `score_rejected` en el volumen de logs | alerta 100200 en el dashboard |
| Frecuencia | 5 líneas `login_fail` con el mismo `username` en < 120 s | alerta 100211 |
| Prompt injection | línea `dialog_blocked` | alerta 100221 |
| FIM | crear y borrar `builds/webgl/.fim-test` | estado en `002.db`; sin alerta por archivo (limitación 4.14) |
| Aislamiento | `nmap -Pn -p 1514,1515,443 100.x.y.z` desde fuera de la tailnet | `filtered` |
| Dashboard | abrir `https://100.x.y.z` solo desde la tailnet | login, sin exposición pública |

### 5.7 Fase 6 — Pruebas con el juego y evidencias

Cuando F5/F6 estén cerradas:

1. Ejecutar las recetas de [`security.md`](security.md) §3 (Burp, replay, fuerza bruta,
   SQLi, XSS, prompt injection, `nmap`).
2. Para cada control, guardar en `evidencias/<control>/`:
   - salida literal del ataque,
   - alerta correlacionada de Wazuh (JSON/CSV exportado, sin secretos),
   - captura del dashboard,
   - corrección aplicada y verificación.
3. Mapear los 10 controles:

| Control | Qué aporta el nodo | Evidencia |
| :--- | :--- | :--- |
| A.8.24 Criptografía | logs `score_rejected` por `bad_signature`/`session_used` | ataque + alerta 100200/100201 |
| A.8.5 Autenticación | `login_fail`/`login_blocked` | fuerza bruta + alerta 100210/100211/100212 |
| A.8.9 Configuración | servidor autoritativo; rechazo de puntaje alterado | edición de IndexedDB + log |
| A.8.20 Redes | `nmap` y aislamiento Docker | salida literal + dashboard |
| A.8.12 DLP | sin secretos en consola/`strings` | captura + `strings` |
| A.8.15 Logging | reglas, correlación y dashboard | alertas + `wazuh-logtest` |
| A.8.28 Codificación | `dialog_blocked`, `register_rejected` | SQLi/XSS/injection + alerta |
| A.8.31 Separación | `app_start` con `env` y token DEV/PROD | prueba DEV→PROD |
| A.8.25 SDLC | Gitleaks (hook/CI), no Wazuh | salida de Gitleaks |
| A.8.19 Instalación | FIM del webroot + `SHA256SUMS`/GPG | cambio de `.wasm` + alerta |

`Sg-DB` es Supabase Cloud: **no** se le puede instalar un agente Wazuh. La vigilancia de la
base se hace con los logs del backend y los audit logs de Supabase, no con un agente.

### 5.8 Fase 7 — Operación y rollback

- **Operación:** el agente y la VM se levantan aparte del juego. Si Wazuh falla, el juego
  sigue; la única pérdida es la evidencia en vivo.
- **Rollback:** `docker compose --profile security stop wazuh-agent`, apagar la VM o
  restaurar un checkpoint de Hyper-V. El juego no se toca.
- **Actualizaciones:** no actualizar Wazuh de versión mayor sin checkpoint y sin repetir
  `wazuh-logtest`; las reglas y el dashboard pueden cambiar.
- **Secretos:** auth key de Tailscale, contraseña de `authd` y `admin` del dashboard fuera
  del repo; `.env.example` solo con placeholders.

---

## 6. Qué dejar preparado ahora (antes de terminar el juego)

1. Recuperar la VM y dejar Tailscale/UFW funcionando.
2. Crear `infra/wazuh/` con `rules/`, `agent-group/` y un `README.md`; el compose ya monta
   `./wazuh`, pero hoy la carpeta no existe.
3. Añadir los volúmenes de logs y el `FileHandler` del backend (BE-12) para que el agente
   tenga algo que leer.
4. Dejar el servicio `wazuh-agent` en el compose, detrás del perfil `security`, con
   `WAZUH_MANAGER` vacío en `.env.example`.
5. Probar reglas con eventos sintéticos; no hace falta esperar a F5/F6.
6. Actualizar `docs/arquitectura.md` §2/§3 y `Plan.md` §9.1 cuando se confirme la opción A.

## 7. Qué no hacer

- No publicar 1514/1515/443 de la VM a Internet.
- No usar el 443 de la VM como entrada del juego: el único 443 público es `Sg-Frontend`.
- No levantar el servicio `security` del compose y la VM al mismo tiempo.
- No meter la auth key de Tailscale ni contraseñas en `infra/`, `.env.example` ni docs.
- No instalar un agente Wazuh en Supabase Cloud.
- No reutilizar literalmente las reglas 100100/100101 del taller: los eventos del juego son
  `score_rejected`, `login_fail`, `login_blocked` y `dialog_blocked`, no los del taller.
- No bloquear el arranque del juego si Wazuh no está disponible: el nodo es de observación.

---

## 8. Checklist de terminado

- [x] VM accesible por SSH directo a `172.18.18.220`; `multipass info/exec` falla por DNS/KVP.
- [ ] Tailnet nueva con tags `game-security`/`game-host` y ACL mínima.
- [x] UFW: 22/443/1514/1515 desde `172.18.16.0/20` y `172.19.224.0/20`; resto denegado. Tailscale pendiente.
- [x] Dashboard con contraseña cambiada; usuario de solo lectura pendiente.
- [x] Agente `sg-game-host` en estado `active` (ID 002).
- [x] Reglas 100200–100250 probadas; `100250` con 36 alertas reales.
- [ ] FIM sobre `/fim/webgl`: estado en `002.db`, sin alerta por archivo en Wazuh 4.14 all-in-one.
- [x] Logs de Nginx llegando al agente; `backend.log` ya se escribe (2026-11-25).
- [ ] `nmap` desde host y VM hecho; falta desde un contenedor de la DMZ.
- [ ] 10 carpetas en `evidencias/`; hay 3 controles parciales + `SEC-12`/`SEC-13`.
- [x] `docs/arquitectura.md` y `Plan.md` actualizados con la desviación de la VM externa.
