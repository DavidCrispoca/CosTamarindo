# A.8.19 — Seguridad en la instalación — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.19 — *Security of information in installation*
> **Fecha de ejecución:** 2026-11-25
> **Herramientas:** `Get-FileHash` (SHA-256), GnuPG 2.5.24
> **Estado:** ⚠️ `SHA256SUMS` regenerado sobre el build con controles de audio (2026-11-25);
> la firma GPG quedó pendiente de re-firma (la clave privada vive en el keyring de la división
> Security y no está disponible en este equipo). El `.asc` actual corresponde a una versión
> anterior del build.
> **Riesgo asociado:** R-07 (reemplazo o manipulación del artefacto desplegado).

---

## 1. Qué se controla

Que el artefacto que se publica sea **exactamente** el que se construyó y auditó, y que un
reemplazo del `.wasm`/`.data` durante la instalación se detecte. La cadena de integridad es:

```
build → SHA256SUMS → (firma GPG) → verificación en el nodo antes de servir
```

Propiedad demostrada: **integridad** del artefacto instalado.

---

## 2. Antes (línea base)

| Campo | Valor |
| :--- | :--- |
| Artefacto | `deploy/render/webgl/Build/` (versionado en el repo) y `builds/webgl/Build/` (no versionado) |
| Estado previo | Sin `SHA256SUMS` ni verificación de integridad del webroot |

---

## 3. Corrección

| Pieza | Cambio | Motivo |
| :--- | :--- | :--- |
| `evidencias/A.8.19/SHA256SUMS` | Sumas SHA-256 de los 4 archivos de `deploy/render/webgl/Build/` | Base de integridad reproducible |
| `evidencias/A.8.19/SHA256SUMS.asc` | Firma GPG separada (ASCII armor) de `SHA256SUMS` | El hash no se puede reemplazar sin invalidar la firma |
| `evidencias/A.8.19/pubkey.asc` | Clave pública GPG `CosTamarindo Security (build signing)` | Verificación pública de la firma |
| `deploy/render/webgl/Build/` | Artefacto versionado en el repo | Punto de comparación contra el build local |

La clave de firma (RSA 2048, solo firma, vigencia 2 años) y su passphrase viven en el
keyring local de la división Security, fuera del repositorio. La firma se verifica con:

```powershell
gpg --verify evidencias/A.8.19/SHA256SUMS.asc evidencias/A.8.19/SHA256SUMS
# -> "Firma correcta de CosTamarindo Security (build signing)"
```

---

## 4. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando | `Get-FileHash -Algorithm SHA256 -LiteralPath deploy\render\webgl\Build\<archivo>` + `gpg --verify SHA256SUMS.asc SHA256SUMS` |
| Resultado esperado | Hash coincide y firma GPG válida |
| Resultado observado | Los hashes coinciden con el build versionado; la firma `.asc` es anterior al último `SHA256SUMS` y este equipo tampoco tiene importada la clave pública, así que no se puede dar por verificada |
| Fecha/hora | 2026-11-25 |

---

## 5. Limitaciones y riesgo residual

- **Sin verificación automática en el despliegue:** falta un paso en el procedimiento de
  publicación que compare el webroot contra `SHA256SUMS` (y la firma). Pendiente de integrar
  en el CI/deploy y con FIM (A.8.15).
- **La clave privada es local.** La firma solo la puede regenerar quien tenga el keyring; la
  pública queda versionada para que cualquiera verifique.
- **`builds/webgl` no está versionado**, por lo que la base de comparación es
  `deploy/render/webgl/Build/` (que sí lo está). Al regenerar el build hay que regenerar y
  volver a firmar este `SHA256SUMS`.

---

## 6. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`SHA256SUMS`](SHA256SUMS) | Sumas SHA-256 de los 4 archivos del build |
| [`SHA256SUMS.asc`](SHA256SUMS.asc) | Firma GPG separada del `SHA256SUMS` |
| [`pubkey.asc`](pubkey.asc) | Clave pública para verificar la firma |

---

## 7. Checklist de cierre

- [x] `SHA256SUMS` generado sobre el artefacto versionado (regenerado el 2026-11-25).
- [ ] Firma GPG del `SHA256SUMS` (pendiente de re-firma tras el rebuild; clave en la división Security).
- [x] Clave pública versionada para verificación.
- [ ] Verificación de integridad automática en el procedimiento de despliegue.
- [x] Regenerar y firmar `SHA256SUMS` tras cada release (regenerado; falta la firma).
