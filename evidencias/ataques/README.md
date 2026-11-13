# Ataques reproducibles — Sg-Security

Esta carpeta contiene los comandos y scripts que producen la evidencia de ataque de cada
control. **No** contiene credenciales, tokens ni claves. Los scripts apuntan a `juego.local`,
`127.0.0.1` o a la IP local de la VM; nunca a Internet.

## Reglas de uso

1. Ejecutar solo contra el entorno local del proyecto.
2. No usar `--no-verify` ni saltar el hook de Gitleaks.
3. Si un ataque modifica datos, restaurar el entorno y dejar constancia en la evidencia.
4. Guardar la salida literal en `evidencias/<control>/`.
5. No pegar secretos reales en los archivos de salida; usar valores de prueba.

## Índice

| Script | Control | Qué hace |
| :--- | :--- | :--- |
| [`nmap.ps1`](nmap.ps1) | A.8.20 | Escanea puertos publicados del host o de la VM. |
| [`tls-headers.ps1`](tls-headers.ps1) | A.8.24 | Muestra cabeceras HTTP y versión TLS negociada. |

## Comandos de referencia

### A.8.20 — aislamiento de red

```powershell
# Desde el host: solo 443 debe estar abierto en el frontend
powershell -File evidencias/ataques/nmap.ps1 -Target 127.0.0.1 -Ports 443,8000,8081,11434

# Desde el host hacia la VM: 22/443/1514/1515 permitidos; 9200/55000 filtrados
powershell -File evidencias/ataques/nmap.ps1 -Target 172.18.18.220 -Ports 22,443,1514,1515,55000,9200

# Desde un contenedor de la DMZ: el backend y la IA no deben ser alcanzables
docker run --rm --network costamarindo-dmz curlimages/curl:8.10.1 `
  -sS --max-time 3 http://sg-backend:8000/api/health
```

### A.8.24 — TLS y cabeceras

```powershell
powershell -File evidencias/ataques/tls-headers.ps1 -Url https://juego.local/healthz
powershell -File evidencias/ataques/tls-headers.ps1 -Url https://juego.local/api/health
```

Para verificar la cadena con OpenSSL y la CA local de mkcert:

```powershell
$caroot = (mkcert -CAROOT).Trim()
& 'C:\Program Files\Git\usr\bin\openssl.exe' s_client `
  -connect 127.0.0.1:443 -servername juego.local -tls1_3 -brief `
  -CAfile (Join-Path $caroot 'rootCA.pem')
```

### A.8.15 — Wazuh y logs

```bash
# En la VM
sudo /var/ossec/bin/agent_control -l
sudo python3 /tmp/extract_alert.py /var/ossec/logs/alerts/2026/Nov/ossec-alerts-13.json 100250
```

Para generar una ráfaga controlada de `429` (no es un ataque de denegación real, es una
prueba de rate limit):

```powershell
$urls = 1..60 | ForEach-Object { 'https://juego.local/api/health' }
& curl.exe --ssl-no-revoke --parallel --parallel-max 60 -s -o NUL @urls
```

## Limitaciones conocidas

- `nmap` en Windows puede mostrar `Could not import all necessary Npcap functions`; se usa
  `-sT -Pn --unprivileged` para que funcione sin privilegios. El resultado se interpreta con
  esa advertencia.
- El build WebGL puede no estar presente en un clon limpio (`builds/` no se versiona). En ese
  caso `https://juego.local/` devuelve `403`, pero las cabeceras y el TLS se pueden verificar
  igual contra `/healthz` y `/api/health`.
- La ráfaga de `429` debe ejecutarse con el frontend arriba y el backend puede estar caído:
  el rate limit de Nginx se aplica antes del proxy.
