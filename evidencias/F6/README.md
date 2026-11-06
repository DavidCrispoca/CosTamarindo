# F6 · Infraestructura de 5 nodos — evidencia

> **Fase:** F6 · infraestructura Docker
> **Fecha:** 2026-11-03
> **Estado:** 🚧 compose completo declarado y validado; `Sg-Frontend` y `Sg-AI` operativos

---

## 1. Qué está arriba

```
$ docker compose -f infra/docker-compose.yml ps
sg-ai        Up (healthy)   11434/tcp
sg-backend   Up (healthy)   8000/tcp
sg-frontend  Up (healthy)   0.0.0.0:443->443/tcp, 0.0.0.0:8081->8080/tcp
```

| Nodo | Estado | Perfil | Puerto publicado |
| :--- | :--- | :--- | :--- |
| `Sg-Frontend` | healthy | *(siempre)* | 443, 8081 (8081 solo en dev) |
| `Sg-AI` | healthy | *(siempre)* | **ninguno** |
| `Sg-Backend` | healthy | `backend` | ninguno (`expose` 8000) |
| `Sg-Security` | declarado | `security` | ninguno |

Actualizado 2026-11-06: el backend ya existe, arranca con `--profile backend` y responde
`/api/health` a través de Nginx.

`Sg-Backend` espera a `backend/Dockerfile` (F5) y `Sg-Security` consume ~3 GB, por eso están
detrás de perfiles: `docker compose up` nunca intenta construir lo que no existe.

---

## 2. Verificación de aislamiento

El objetivo de F6 es que **desde fuera solo se vea el juego**.

| Comprobación | Comando | Resultado |
| :--- | :--- | :--- |
| Puerto de IA publicado | `docker port sg-ai 11434` | `no public port published` ✅ |
| Puerto del backend publicado | `docker compose port sg-backend 8000` | sin salida ✅ |
| Ollama desde el host | `Invoke-WebRequest http://127.0.0.1:11434` | no alcanzable ✅ |
| API por HTTPS | `curl https://juego.local/api/health` | `200` con `database: true` ✅ |
| Redes de `sg-ai` | `docker inspect sg-ai` | solo `costamarindo-ai` ✅ |
| Red `ai` | `docker-compose.yml` | `internal: true` ✅ |
| Juego por HTTPS | `curl --ssl-no-revoke https://juego.local/healthz` | `200` en 0,08 s ✅ |
| Sintaxis del compose | `docker compose config --quiet` | exit `0` ✅ |

---

## 3. Comprobación de extremo a extremo del build

Tras recompilar con los scripts nuevos, se sirvió el build desde el contenedor y se midió con
`tools/dev-server/check.mjs` (Playwright sobre el Edge instalado):

| Métrica | Valor | DoD |
| :--- | ---: | :--- |
| `unityReady` | `true` | — |
| Runtime listo | **1,5 s** | < 10 s ✅ |
| Carga total | 6,5 s | — |
| Transferencia | **16,2 MB** | ≤ 30 MB ✅ |
| Errores HTTP | **0** | 0 ✅ |
| Errores de consola | **0** | 0 ✅ |
| Advertencias de consola | **0** | 0 ✅ |
| Canvas | 960×600 | ✅ |

```json
{"url":"https://juego.local/","unityReady":true,"loadSeconds":"1.5","secondsTotal":"6.5",
 "transferMB":16.2,"canvas":{"width":960,"height":600},
 "httpErrors":[],"consoleErrors":[],"consoleWarnings":[]}
```

Captura: [`evidencias/F3/screenshot-final.png`](../F3/screenshot-final.png)

---

## 4. La trampa de esta fase

**`internal: true` impide descargar el modelo de IA.**

`docker compose exec ai ollama pull qwen2.5:3b` falla:

```
Error: pull model manifest: dial tcp: lookup registry.ollama.ai on 127.0.0.11:53: server misbehaving
```

No es un error de configuración: **es el aislamiento funcionando**. La red `ai` no tiene salida a
Internet, que es justo lo que exige A.8.20 (si el NPC no puede salir, no hay exfiltración desde
el modelo).

**Procedimiento correcto** (implementado en `infra/scripts/pull-model.sh`):

```bash
docker network connect bridge sg-ai      # salida temporal
docker compose exec -T ai ollama pull qwen2.5:3b
docker network disconnect bridge sg-ai   # vuelve a quedar aislado
```

Verificado: el modelo (`qwen2.5:3b`, 1,9 GB) queda en el volumen `costamarindo-ai-models` y
`sg-ai` vuelve a tener **solo** la red `costamarindo-ai`.

---

## 5. Scripts de operación

| Script | Función |
| :--- | :--- |
| `infra/scripts/up.sh` / `.ps1` | Levanta el entorno con los perfiles pedidos |
| `infra/scripts/down.sh` | Apaga; `--purge` borra volúmenes |
| `infra/scripts/logs.sh` | Logs de un nodo en vivo |
| `infra/scripts/deploy.sh` | Build → TLS → up → salud → aislamiento |
| `infra/scripts/pull-model.sh` | Descarga el modelo con salida temporal |

---

## 6. Pendiente para cerrar F6

- [x] `backend/Dockerfile` (F5) para activar el perfil `backend` y validar el proxy `/api/` (`BE-08`).
- [x] `docker-compose.override.yml` dev/prod (en prod, solo `443`; sin `8081`) (`BE-11`).
- [ ] `nmap` desde el host y desde otro contenedor (F8) para la evidencia formal de A.8.20 (`SEC-06`).
- [ ] Wazuh con reglas 100100/100101 (F8) para activar el perfil `security` (`SEC-08`).
- [ ] Revisar la conexión de `frontend` a `mgmt`: hoy no hay ningún servicio que reciba esos logs.
