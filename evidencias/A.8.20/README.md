# A.8.20 — Seguridad de redes — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.20 — *Network security*
> **Fecha de ejecución:** 2026-11-13
> **Herramienta:** Nmap 7.80, Docker Compose
> **Estado:** Parcial — aislamiento y puertos verificados; falta prueba desde un segundo
> contenedor y revisión de reglas de la VM.
> **Riesgo asociado:** R-09 (abuso por ráfaga) y R-03 (acceso no autorizado).

---

## 1. Qué se controla

Que el entorno del juego solo exponga el puerto **443** al exterior y que los servicios
internos (`Sg-Backend:8000`, `Sg-AI:11434`, Wazuh API `55000` y OpenSearch `9200`) no sean
alcanzables desde el host ni desde Internet. La VM `Sg-Security` es un nodo separado: su
dashboard y su API no se publican; solo se permite el acceso necesario desde la red local.

---

## 2. Línea base

| Elemento | Valor |
| :--- | :--- |
| Frontend | `sg-frontend`, Nginx, `443` publicado |
| Backend | `sg-backend`, `expose: 8000`, sin `ports` |
| IA | `sg-ai`, `expose: 11434`, sin `ports` |
| VM | `sg-security`, `172.18.18.220` (Default Switch) |
| Redes internas | `app`, `ai`, `mgmt` con `internal: true` |

La configuración declarada está en `infra/docker-compose.yml`; los perfiles `backend` y
`security` no se levantan con `docker compose up`.

---

## 3. Ataque / verificación

### 3.1 Host → frontend

```powershell
nmap -sT -Pn --unprivileged -p 443,8081,8000,11434 127.0.0.1
```

Resultado (ver [`01-nmap-host.txt`](01-nmap-host.txt)):

| Puerto | Estado | Lectura |
| ---: | :--- | :--- |
| 443 | open | Único puerto público del juego |
| 8081 | filtered | El override de dev no está activo |
| 8000 | filtered | Backend no publicado |
| 11434 | filtered | Ollama no publicado |

### 3.2 Host → VM `sg-security`

```powershell
nmap -sT -Pn --unprivileged -p 22,443,1514,1515,55000,9200 172.18.18.220
```

Resultado (ver [`02-nmap-vm.txt`](02-nmap-vm.txt)):

| Puerto | Estado | Lectura |
| ---: | :--- | :--- |
| 22 | open | SSH por clave, solo desde la red local |
| 443 | open | Dashboard Wazuh, no es la entrada del juego |
| 1514 | open | Agente → manager |
| 1515 | open | Registro de agentes |
| 55000 | filtered | Wazuh API solo en localhost |
| 9200 | filtered | OpenSearch solo en localhost |

### 3.3 Configuración declarada

Ver [`03-docker-ports.txt`](03-docker-ports.txt) y [`04-network-internal.txt`](04-network-internal.txt):

- `frontend`: `ports=[443:443]`, sin `expose`.
- `backend`: `ports=(ninguno)`, `expose=[8000]`.
- `ai`: `ports=(ninguno)`, `expose=[11434]`.
- `wazuh-agent` y `security`: sin puertos publicados.
- `costamarindo-app`, `costamarindo-ai` y `costamarindo-mgmt`: `internal=True`.

---

## 4. Corrección aplicada

- Se fijaron subredes explícitas `10.80.0.0/16`–`10.85.0.0/16` para evitar solapamientos con
  Hyper-V.
- El backend y la IA usan `expose:` y no `ports:`.
- El frontend es el único con `ports: 443`.
- La VM tiene UFW con `22/443/1514/1515` desde `172.18.16.0/20` y `172.19.224.0/20`;
  `55000/9200` quedan en localhost.

---

## 5. Verificación posterior

- `docker ps` muestra solo `sg-frontend` con `443` publicado.
- Nmap confirma `8000` y `11434` filtrados desde el host.
- La VM confirma `55000` y `9200` filtrados desde el host.
- `docker network inspect` confirma `internal=True` en `app`, `ai` y `mgmt`.

---

## 6. Limitaciones y riesgo residual

- **Falta la prueba desde otro contenedor.** El README de ataques deja el comando con
  `docker run --rm --network costamarindo-dmz curlimages/curl ...`; no se ejecutó en esta
  pasada para no descargar una imagen nueva. Es el siguiente paso para cerrar el control.
- **8081 no está publicado** porque el override de desarrollo no se cargó al usar
  `-f docker-compose.yml`. En la demo se debe decidir si se usa el override o no.
- **La VM no está en una tailnet todavía.** El acceso es por Default Switch; Tailscale se
  difiere hasta tener los cinco nodos.
- **Nmap en Windows** muestra una advertencia de Npcap y usa `connect()`. El resultado es
  válido para puertos TCP, pero no reemplaza un escaneo desde una red externa.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-nmap-host.txt`](01-nmap-host.txt) | Puertos del frontend desde el host |
| [`02-nmap-vm.txt`](02-nmap-vm.txt) | Puertos de la VM desde el host |
| [`03-docker-ports.txt`](03-docker-ports.txt) | `ports` vs `expose` en Compose |
| [`04-network-internal.txt`](04-network-internal.txt) | Redes Docker internas |

---

## 8. Checklist de cierre

- [x] Nmap desde el host al frontend.
- [x] Nmap desde el host a la VM.
- [x] Configuración de Compose y redes internas.
- [ ] Nmap desde un contenedor de la DMZ.
- [ ] Repetir después de cada cambio de red o de perfil.
