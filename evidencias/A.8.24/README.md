# A.8.24 — Uso de criptografía — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.24 — *Use of cryptography*
> **Fecha de ejecución:** 2026-11-13
> **Herramientas:** mkcert 1.4.4, OpenSSL 3.5.4, Nginx 1.29, curl 8.22
> **Estado:** Parcial — TLS local y cabeceras verificados; falta cifrado en reposo y
> gestión de claves a largo plazo.
> **Riesgo asociado:** R-05 (intercepción o suplantación TLS).

---

## 1. Qué se controla

Que el juego se sirva por HTTPS con un certificado confiable en el equipo de desarrollo,
que Nginx acepte solo TLS 1.2/1.3, que rechace versiones obsoletas y que entregue cabeceras
de seguridad (HSTS, CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`,
`Permissions-Policy` y `Cross-Origin-Opener-Policy`).

---

## 2. Antes

| Campo | Valor |
| :--- | :--- |
| Certificado | Autofirmado con OpenSSL de Git |
| Confianza del navegador | No confiable (`ERR_CERT_AUTHORITY_INVALID`) |
| `mkcert` | No instalado |
| Cabeceras | Presentes en Nginx, pero el certificado no era válido para el navegador |

El estado anterior está documentado en `infra/README.md` y en el handoff de Security.

---

## 3. Corrección aplicada

1. Se instaló `mkcert` con Winget:
   `winget install --id FiloSottile.mkcert -e`.
2. Se ejecutó `infra/scripts/tls.ps1`, que generó:
   - `infra/tls/juego.local.pem`
   - `infra/tls/juego.local-key.pem`
   - CA local en `%LOCALAPPDATA%\mkcert\rootCA.pem`
3. Se agregó la CA al almacén de usuario (`certutil -user -addstore`).
4. Se agregó `127.0.0.1 juego.local` a `C:\Windows\System32\drivers\etc\hosts`.
5. Se reinició `sg-frontend` para que Nginx cargara el certificado nuevo.

El certificado cubre `juego.local`, `localhost`, `127.0.0.1` y `::1`; vence el
**13 de febrero de 2029**. La CA local vence el **13 de noviembre de 2036**.

---

## 4. Verificación

### 4.1 TLS 1.3 y 1.2

```powershell
$caroot = (mkcert -CAROOT).Trim()
& 'C:\Program Files\Git\usr\bin\openssl.exe' s_client `
  -connect 127.0.0.1:443 -servername juego.local -tls1_3 -brief `
  -CAfile (Join-Path $caroot 'rootCA.pem')
```

Resultado (ver [`02-tls-versions.txt`](02-tls-versions.txt)):

| Versión | Resultado |
| :--- | :--- |
| TLS 1.3 | `TLS_AES_256_GCM_SHA384`, `Verification: OK` |
| TLS 1.2 | `ECDHE-RSA-AES256-GCM-SHA384`, `Verification: OK` |
| TLS 1.1 | Rechazado (`no protocols available`) |

### 4.2 Cabeceras

```powershell
curl.exe --ssl-no-revoke -D - -o NUL https://juego.local/
curl.exe --ssl-no-revoke -D - -o NUL https://juego.local/api/health
```

Resultado (ver [`01-tls-headers.txt`](01-tls-headers.txt)):

| Cabecera | Valor |
| :--- | :--- |
| `Strict-Transport-Security` | `max-age=31536000` |
| `Content-Security-Policy` | `script-src 'self' 'wasm-unsafe-eval' 'sha256-...'; default-src 'self'; ...` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=()` |
| `Cross-Origin-Opener-Policy` | `same-origin` |

> `https://juego.local/` devuelve `403` porque `builds/webgl` está vacío en este clon; no es
> un fallo de TLS. Las cabeceras se verifican igual en `/` y en `/api/health`.

### 4.3 Certificado

Ver [`03-certificate.txt`](03-certificate.txt). La cadena se valida con la CA local de
mkcert; `curl` sin `--ssl-no-revoke` puede fallar por la comprobación de revocación de
Schannel, no por falta de confianza.

---

## 5. Limitaciones y riesgo residual

- **Certificado local:** solo es confiable en el equipo donde se instaló la CA de mkcert.
  Para una demo en otro equipo hay que ejecutar `tls.ps1` o instalar la CA.
- **No hay cifrado en reposo** para el build, los logs o los backups. Es un pendiente de
  A.8.24 y A.8.13.
- **No hay gestión formal de claves** (rotación, custodia, revocación). El certificado y la
  clave viven en `infra/tls/`, que está en `.gitignore`.
- **HSTS sin `includeSubDomains`:** suficiente para `juego.local`; no se agrega para no
  afectar otros servicios locales.
- **TLS 1.2 se mantiene** por compatibilidad; TLS 1.1 y anteriores están deshabilitados.

---

## 6. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`01-tls-headers.txt`](01-tls-headers.txt) | Cabeceras HTTP de `/` y `/api/health` |
| [`02-tls-versions.txt`](02-tls-versions.txt) | TLS 1.3 y 1.2 aceptados; TLS 1.1 rechazado |
| [`03-certificate.txt`](03-certificate.txt) | CA, vigencia, SANs y archivos generados |

---

## 7. Checklist de cierre

- [x] `mkcert` instalado y CA confiada en el usuario.
- [x] Certificado para `juego.local` y `localhost`.
- [x] Nginx sirve TLS 1.2/1.3.
- [x] TLS 1.1 rechazado.
- [x] Cabeceras de seguridad verificadas.
- [ ] Repetir en un equipo limpio y documentar la instalación de la CA.
- [ ] Definir cifrado en reposo y gestión de claves.
