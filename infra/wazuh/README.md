# `infra/wazuh/` — Sg-Security

Reglas, configuración de agentes y notas de operación del nodo **Sg-Security**.
La VM que lo ejecuta es `sg-security` (Multipass, Ubuntu 24.04, Wazuh 4.14.10).

> **Reconstrucción 2026-11-10:** la VM anterior `sg-jacome-security` quedó inaccesible
> (sin contraseña de consola/SSH) y se reemplazó por `sg-security`, creada desde cero
> con [`provision-security-node.sh`](provision-security-node.sh). El procedimiento es
> reproducible y no contiene secretos.

> **Alcance actual:** el nodo queda montado, con logs reales de Nginx y reglas
> disparando en el manager. No se usa todavía en la partida: el juego no está
> cerrado y Bearer/SonarQube quedan para el final. La tailnet se crea cuando estén
> los cinco nodos; hasta entonces la VM queda fuera de la tailnet vieja.

## Estado verificado (2026-11-13)

- VM `sg-security` en `Running`, IP **`172.18.18.220`** (Default Switch), Ubuntu
  24.04.5, 4 vCPU / 8 GB / 30 GB. El host quedó con `172.18.16.1/20`; la IP
  secundaria vieja `172.30.224.2/20` sigue en el adaptador y requiere una consola
  elevada para retirarla.
- Wazuh **4.14.10** all-in-one. Dashboard en `443`; API `55000` y OpenSearch `9200`
  escuchan solo en `127.0.0.1`. UFW permite `22/443/1514/1515` desde
  `172.18.16.0/20` y `172.19.224.0/20`.
- Redes Docker con subredes explícitas `10.80.0.0/16`–`10.85.0.0/16` para no
  solaparse con Hyper-V. El agente usa `costamarindo-mgmt` (`10.84.0.0/16`) y
  `costamarindo-siem` (`10.85.0.0/16`).
- Reglas cargadas y probadas con eventos reales: `100200` (nivel 7), `100210` (5) y
  `100250` (36 alertas). `100240/100241` están cargadas sobre las reglas base
  550/553/554.
- Agente `sg-game-host` (**ID 002**) **Active**, grupo `game`. El ID 001 fue purgado
  por `authd` al re-registrar; la clave del agente se conserva en el contenedor.
- `multipass info/exec` falla por DNS/KVP (`sg-security.mshome.net` apunta a la IP
  vieja). El acceso operativo es SSH directo a `172.18.18.220` con la clave de
  Multipass; no hace falta consola Hyper-V.
- **Logs reales de Nginx:** el frontend escribe `access.log` como archivo regular
  (no symlink a stdout) y el agente lo lee desde el volumen `frontend-logs`. Una
  ráfaga contra `/api/` produjo `429` y la regla `100250` disparó **36 alertas
  reales** en el manager.
- **FIM:** el agente detecta cambios en `/fim/webgl` y el manager guarda el estado
  en `002.db` (`fim_entry` con `changes=1`), pero Wazuh 4.14 en este all-in-one no
  emite la alerta por archivo: solo llegan `scan_start`/`scan_end` y el `fimsync`.
  El FIM local del manager muestra la misma limitación. `100240/100241` quedan
  cargadas y documentadas; no se presenta una alerta FIM end-to-end como evidencia.
- **Backend:** imagen reconstruida (`latest`) y volumen `backend-logs` listo. El 2026-11-25 el
  contenedor arrancó con las variables de `.env` (`SUPABASE_URL`, `SUPABASE_PUBLISHABLE_KEY`,
  `SUPABASE_SECRET_KEY`, `SUPABASE_DB_URL`, `BACKEND_JWT_SECRET`, `PIN_PEPPER` y
  `SESSION_MASTER_KEY`), quedó `healthy` y el `FileHandler` ya escribe
  `backend.log`; el `localfile` del agente está configurado. Esas claves no se versionan.
- Credenciales fuera del repo: en la VM `/root/wazuh-registration-password.txt` y
  `/root/wazuh-dashboard-admin-password.txt`; en el host `.env` (ignorado por git) y
  `C:\Users\yoann\.config\costamarindo\wazuh-dashboard-admin.txt`.
- La VM vieja `sg-jacome-security` fue eliminada el 2026-11-10 con `multipass delete --purge`.
- **Tailscale** se difiere hasta tener los cinco nodos; **Sg-IA aún no se inicia**.
  La VM queda lista para conectarse cuando exista la tailnet del juego.

## Contenido

| Archivo | Destino en el manager | Para qué |
| :--- | :--- | :--- |
| `provision-security-node.sh` | Se ejecuta en la VM nueva | Instala Wazuh all-in-one, UFW, SSH y copia reglas/grupo |
| `rules/costamarindo_rules.xml` | `/var/ossec/etc/rules/costamarindo_rules.xml` | Reglas 100200–100250 del juego |
| `agent-group/game.conf` | `/var/ossec/etc/shared/game/agent.conf` | Logs del backend/Nginx y FIM del webroot |

## Crear el nodo desde cero

```powershell
multipass launch 24.04 --name sg-security --cpus 4 --memory 8G --disk 30G --timeout 1800

multipass exec sg-security -- mkdir -p /tmp/costamarindo
multipass transfer infra/wazuh/provision-security-node.sh sg-security:/tmp/provision-security-node.sh
multipass transfer infra/wazuh/rules/costamarindo_rules.xml sg-security:/tmp/costamarindo/costamarindo_rules.xml
multipass transfer infra/wazuh/agent-group/game.conf sg-security:/tmp/costamarindo/game.conf

# El instalador de Wazuh tarda; se lanza en segundo plano y se sigue el log.
multipass exec sg-security -- sudo bash -c 'ALLOWED_SUBNET=172.19.224.0/20 nohup bash /tmp/provision-security-node.sh > /var/log/costamarindo-provision.log 2>&1 & echo PID:$!'
multipass exec sg-security -- sudo tail -f /var/log/costamarindo-provision.log
```

Al terminar debe aparecer `PROVISION_OK`. La contraseña de registro de agentes queda
en `/root/wazuh-registration-password.txt` (modo 600) y las credenciales del dashboard
en `/root/wazuh-install-files.tar`. **No copiar ninguna de las dos al repositorio.**

> **Limpieza:** el log de aprovisionamiento y `/var/log/wazuh-install.log` pueden
> contener credenciales generadas. Una vez validado el nodo, borrarlos. Si el
> password de `admin` quedó expuesto, rotarlo con
> `/usr/share/wazuh-indexer/plugins/opensearch-security/tools/wazuh-passwords-tool.sh -u admin`
> y guardar el nuevo valor fuera del repo. En esta VM ya se rotó y los logs se
> eliminaron.

La subred `172.19.224.0/20` es la del Default Switch de este equipo; si cambia, pasar
el valor correcto en `ALLOWED_SUBNET`. Cuando exista la tailnet se añaden reglas UFW
para `tailscale0` y se puede retirar la regla de la subred local.

Las reglas **no** reutilizan las 100100/100101 del taller. El rango del juego es
100200–100299 y se documenta en el propio XML.

## Instalación en la VM

Copiar los dos archivos desde el repo (por SSH o `multipass transfer`) y dejarlos
con propietario `wazuh:wazuh`:

```bash
sudo install -o wazuh -g wazuh -m 0640 \
  rules/costamarindo_rules.xml /var/ossec/etc/rules/costamarindo_rules.xml

sudo mkdir -p /var/ossec/etc/shared/game
sudo install -o wazuh -g wazuh -m 0640 \
  agent-group/game.conf /var/ossec/etc/shared/game/agent.conf

sudo /var/ossec/bin/wazuh-control restart
sudo /var/ossec/bin/wazuh-control status
```

Comprobar que el manager escucha a los agentes:

```bash
sudo grep -A4 '<remote>' /var/ossec/etc/ossec.conf   # secure 1514/tcp
sudo grep -A6 '<auth>'   /var/ossec/etc/ossec.conf   # authd 1515/tcp
sudo ss -tlnp | grep -E '1514|1515|443|55000|9200'
```

`55000` (API de Wazuh) y `9200` (OpenSearch) deben escuchar solo en
`127.0.0.1`; `1514/1515/443` solo en `tailscale0` (o, mientras no exista la
tailnet, en la subred del Default Switch).

## Validar reglas sin el juego

```bash
sudo /var/ossec/bin/wazuh-logtest
```

Pegar una línea como:

```json
{"ts":"2026-11-10T18:00:00.000Z","level":"INFO","logger":"costamarindo.audit","event":"score_rejected","request_id":"test-1","user_id":"00000000-0000-0000-0000-000000000000","reason":"bad_signature","ip":"203.0.113.0"}
```

Resultado esperado: decoder `json` y regla `100200`.

Casos mínimos:

| Evento | Regla | Nivel |
| :--- | :--- | :--- |
| `score_rejected` | 100200 | 7 |
| 5 × `score_rejected` mismo `user_id` en 120 s | 100201 | 10 |
| `login_fail` | 100210 | 5 |
| 5 × `login_fail` mismo `username` en 120 s | 100211 | 10 |
| `login_blocked` | 100212 | 9 |
| `register_rejected` | 100220 | 6 |
| `dialog_blocked` | 100221 | 9 |
| 10 × `dialog_blocked` mismo `user_id` en 60 s | 100222 | 12 |
| cambio/alta en `/fim/webgl` | 100240 | 10 |
| borrado en `/fim/webgl` | 100241 | 12 |
| Nginx con `status=429` | 100250 | 5 |

## Logs reales de Nginx (verificado)

El servicio `frontend` arranca Nginx con un `command` que reemplaza los symlinks
`access.log`/`error.log` de la imagen por archivos regulares en el volumen
`frontend-logs`. Sin eso, el agente vería un symlink a `/dev/stdout` y no podría leer
el log. Para generar una alerta real de rate limit:

```powershell
$urls = 1..40 | ForEach-Object { 'https://localhost/api/health' }
curl.exe -k -s --parallel --parallel-max 40 --max-time 10 -o NUL -w "%{http_code}`n" @urls
```

El `/healthz` tiene `access_log off`; por eso la prueba usa `/api/`. El manager debe
mostrar `100250` con `location: /var/log/nginx/access.log` y `agent.id: 002`.
Verificación:

```bash
sudo grep -h 100250 /var/ossec/logs/alerts/alerts.json | tail -n 3
```

## Agente del host del juego

El agente vive en `infra/docker-compose.yml`, detrás del perfil `security`:

```bash
cd infra
docker compose --env-file ../.env --profile security up -d wazuh-agent
docker exec -it sg-wazuh-agent /var/ossec/bin/wazuh-control status
```

La imagen `wazuh/wazuh-agent:4.14.10` lee `WAZUH_MANAGER_SERVER` (no solo
`WAZUH_MANAGER`) para escribir `<address>` en `ossec.conf`; el compose define ambos a
partir de `WAZUH_MANAGER`. Si cambia la IP del manager, recrear el contenedor.

Variables en el `.env` real del host (nunca en el repo):

```dotenv
WAZUH_MANAGER=<IP-o-nombre-del-manager>
WAZUH_REGISTRATION_PASSWORD=<clave-de-authd>
```

`WAZUH_MANAGER` puede ser la IP de la tailnet o, mientras la tailnet no exista,
la IP del Default Switch (`172.18.18.220` en esta sesión). Si cambia la IP, primero
se actualiza el `.env` y el `<address>` dentro del contenedor, y se reinicia sin
perder la clave del agente:

```bash
docker exec sg-wazuh-agent sed -i 's/<IP-vieja>/<IP-nueva>/g' /var/ossec/etc/ossec.conf
docker restart sg-wazuh-agent
```

Recrear con `--force-recreate` solo si se acepta un ID de agente nuevo o si la clave
`client.keys` está persistida en un volumen. En esta sesión se conservó el **ID 002**.

## Pruebas de FIM y limitación conocida

Con el agente activo y `/fim/webgl` montado (en el host es `builds/webgl`):

```powershell
# En el host, desde la raíz del repo
Set-Content builds/webgl/.fim-test "prueba"
# Esperar el escaneo periódico (hasta 300 s) o reiniciar el agente:
docker restart sg-wazuh-agent
```

El bind mount de Windows no propaga `inotify`; por eso el grupo `game` usa escaneo
programado cada 300 s (`frequency=300`, sin `realtime`). En esta VM el agente **sí**
detecta el archivo y el manager guarda el estado:

```bash
sudo python3 - <<'PY'
import sqlite3
con = sqlite3.connect('/var/ossec/queue/db/002.db')
for row in con.execute("SELECT full_path, changes, date, size FROM fim_entry WHERE full_path LIKE '%/fim/webgl%'"):
    print(row)
PY
```

Debe aparecer la ruta con `changes=1`. Sin embargo, Wazuh 4.14 en este all-in-one
**no emite la alerta por archivo**: solo llegan `scan_start`/`scan_end` y el `fimsync`.
El FIM local del manager muestra la misma limitación. Por eso `100240/100241` quedan
cargadas pero **no se presenta una alerta FIM end-to-end como evidencia**. No se debe
afirmar lo contrario; la evidencia disponible es el `fim_entry` del agente y el estado
del manager.

Para comprobar que las reglas están cargadas:

```bash
sudo grep -n '100240\|100241' /var/ossec/etc/rules/costamarindo_rules.xml
sudo /var/ossec/bin/wazuh-logtest   # solo para eventos JSON de negocio, no FIM
```

## Cuidados

- No publicar `1514`, `1515` ni `443` de la VM a Internet.
- El `443` de la VM es el dashboard de Wazuh, no la entrada del juego.
- No levantar la VM y el servicio `security` del compose a la vez (este último
  vive en el perfil `security-local`).
- No poner contraseñas ni auth keys en este directorio: solo placeholders.
- Si Wazuh falla, el juego sigue: el agente está detrás del perfil `security` y
  no hay `depends_on` del juego hacia él.
