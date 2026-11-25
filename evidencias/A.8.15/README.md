# A.8.15 — Registro de eventos — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.15 — *Logging*
> **Fecha de ejecución:** 2026-11-13
> **Nodo:** VM `sg-security` (`172.18.18.220`, Ubuntu 24.04.5, Wazuh 4.14.10 all-in-one)
> **Agente:** `sg-game-host` (ID **002**, grupo `game`)
> **Estado:** Parcial — logs de Nginx y regla `100250` verificados; FIM guarda estado sin
> alerta por archivo. **Actualización 2026-11-25:** el backend ya arranca healthy y
> `backend.log` se escribe en el volumen `costamarindo-backend-logs`; falta capturar los
> eventos de negocio en el manager. El cuerpo de abajo conserva el estado del 2026-11-13.
> **Riesgo asociado:** R-04 (pérdida o manipulación de logs).

---

## 1. Qué se controla

Que los eventos relevantes del juego y del nodo de seguridad se registren, se envíen a un
SIEM separado y permitan reconstruir qué pasó: peticiones HTTP, errores, rate limit, cambios
en el webroot y eventos de autenticación. El SIEM no debe estar en la ruta de la partida: si
Wazuh falla, el juego sigue funcionando.

---

## 2. Arquitectura verificada

```text
Host Docker
  sg-frontend (Nginx) -- access.log JSON --+
  sg-backend (FastAPI) -- backend.log -----+--> sg-wazuh-agent (ID 002)
                                                   |
                                                   | 1514/tcp (eventos)
                                                   | 1515/tcp (registro)
                                                   v
                                      VM sg-security
                                      Wazuh manager + indexer + dashboard
                                      reglas 100200-100250 + FIM
```

- El agente inicia la conexión; la VM no entra a las redes Docker.
- `mgmt` es `internal: true`; `siem` existe solo para la salida del agente.
- El dashboard de Wazuh escucha en `443` de la VM y no es la entrada del juego.

---

## 3. Estado de los servicios

Ver [`01-wazuh-status.txt`](01-wazuh-status.txt).

| Componente | Estado |
| :--- | :--- |
| `wazuh-manager` | `active` |
| `wazuh-indexer` | `active` |
| `wazuh-dashboard` | `active` |
| Agente `002` | `Active`, `Wazuh v4.14.10`, grupo `game` |
| UFW | `22/443/1514/1515` desde `172.18.16.0/20` y `172.19.224.0/20`; `55000/9200` localhost |
| Puertos en escucha | `443`, `1514`, `1515`, `22`; `55000` y `9200` en `127.0.0.1` |

El Default Switch cambió de `172.19.224.0/20` a `172.18.16.0/20`; se agregaron reglas UFW
para la red nueva y se conservaron las anteriores. El agente se actualizó a
`WAZUH_MANAGER=172.18.18.220` sin volver a registrarlo (conserva el ID `002`).

---

## 4. Evidencia de logs reales: regla `100250`

Se enviaron 60 peticiones concurrentes a `https://juego.local/api/health` con
`curl --parallel`. Nginx respondió `502` (backend caído) y `429` (rate limit). El agente leyó
`/var/log/nginx/access.log` y el manager disparó **36 alertas** de la regla `100250`
(`CosTamarindo: Nginx aplico rate limit (HTTP 429)`).

> **Nota de cierre:** el contador siguió subiendo con el tráfico normal; al verificar el
> 2026-11-13 había 48 alertas. La captura y el análisis se hicieron con 36.

Ver [`02-100250-alert.json`](02-100250-alert.json). Extracto:

```json
{
  "rule": {
    "level": 5,
    "description": "CosTamarindo: Nginx aplico rate limit (HTTP 429)",
    "id": "100250",
    "groups": ["costamarindo"]
  },
  "agent": {"id": "002", "name": "sg-game-host", "ip": "10.85.0.2"},
  "location": "/var/log/nginx/access.log"
}
```

Conteo del día: `100250=36`.

---

## 5. FIM del webroot: estado guardado, sin alerta por archivo

Se creó un archivo de prueba en el webroot montado:

```powershell
Set-Content builds\webgl\.fim-evidence-test 'FIM evidence test 2026-11-13'
docker restart sg-wazuh-agent
```

El agente escaneó `/fim/webgl` y el manager guardó el estado en `002.db`:

| Campo | Valor |
| :--- | :--- |
| `full_path` | `/fim/webgl/.fim-evidence-test` |
| `type` | `file` |
| `changes` | `1` |
| `size` | `30` |
| `sha256` | `1f0fc6114071feeaccc6545d52216c92445599fe2e386b1ae3a2a5d3a5aa2496` |

Ver [`03-fim-state.txt`](03-fim-state.txt). El agente también conserva la entrada en su
`fim.db` local (`db_status_field_dm=1`).

**Limitación:** en este despliegue Wazuh 4.14 all-in-one, el manager **no emite una alerta
por archivo** en `alerts.json` para el cambio de `/fim/webgl` (`grep -c 'fim/webgl'` = 0). El
estado sí queda persistido y es consultable. No es un fallo de la regla ni del agente: es una
limitación del all-in-one, documentada y aceptada como riesgo residual R-04.

El archivo de prueba se eliminó después de capturar la evidencia; el directorio
`builds/webgl` quedó limpio.

---

## 6. `backend.log`: pendiente al 2026-11-13 — **desbloqueado el 2026-11-25**

El 2026-11-13 el backend tenía `FileHandler` y el volumen `backend-logs`, pero no arrancaba
porque faltaban las claves de Supabase/JWT en `.env`:

```text
pydantic_core._pydantic_core.ValidationError: 7 validation errors for Settings
```

**Estado actual:** el backend arranca healthy y `backend.log` se escribe. Lo que queda es
generar los eventos de negocio y confirmar su llegada al manager:

1. ~~Levantar `sg-backend` con el perfil `backend`.~~ Hecho.
2. Generar tráfico real de login, sesión y puntaje.
3. Verificar `backend.log` dentro del volumen y su llegada al manager.
4. Cerrar las reglas de negocio con alertas reales.

---

## 7. Reproducción

```powershell
# Estado de Wazuh y agente
ssh -i C:\Users\yoann\.ssh\multipass_id_rsa ubuntu@172.18.18.220 `
  "sudo systemctl is-active wazuh-manager wazuh-indexer wazuh-dashboard; `
   sudo /var/ossec/bin/agent_control -l"

# Ráfaga controlada para 100250
$urls = 1..60 | ForEach-Object { 'https://juego.local/api/health' }
& curl.exe --ssl-no-revoke --parallel --parallel-max 60 -s -o NUL @urls

# Conteo de alertas
ssh -i C:\Users\yoann\.ssh\multipass_id_rsa ubuntu@172.18.18.220 `
  "sudo python3 /tmp/extract_alert.py /var/ossec/logs/alerts/2026/Nov/ossec-alerts-13.json 100250"
```

---

## 8. Limitaciones y riesgo residual

- **FIM sin alerta por archivo** en Wazuh 4.14 all-in-one. Compensación: estado en `002.db`,
  revisión manual y alertas de logs. Riesgo residual R-04 aceptado.
- **`backend.log` ya existe** (2026-11-25) en el volumen `costamarindo-backend-logs`; falta la
  alerta de las reglas de negocio en el manager.
- **Tailscale no está configurado.** El transporte actual es Default Switch; la VM no se
  publica a Internet.
- **Retención de logs** no está definida; los archivos diarios se conservan en la VM.
- **No hay prueba de manipulación del log** ni de envío a un SIEM externo.

---

## 9. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-wazuh-status.txt`](01-wazuh-status.txt) | Servicios, agente 002, UFW, puertos y conteo 100250 |
| [`02-100250-alert.json`](02-100250-alert.json) | Alerta real de Nginx 429 con `location` y agente |
| [`03-fim-state.txt`](03-fim-state.txt) | Estado FIM en manager y agente; ausencia de alerta por archivo |

---

## 10. Checklist de cierre

- [x] Wazuh manager, indexer y dashboard activos.
- [x] Agente `002` activo y en el grupo `game`.
- [x] Logs reales de Nginx y regla `100250` con 36 alertas.
- [x] FIM guarda estado de `/fim/webgl` en `002.db`.
- [x] Limitación de alerta FIM documentada.
- [x] `backend.log` real escribiéndose (2026-11-25).
- [ ] Reglas de negocio del backend con alertas reales en el manager.
- [ ] Retención y protección de logs.
- [ ] Tailscale o transporte definitivo.
