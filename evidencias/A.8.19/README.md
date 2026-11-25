# A.8.19 — Seguridad en la instalación — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.19 — *Security of information in installation*
> **Fecha de ejecución:** 2026-11-25
> **Herramientas:** `Get-FileHash` (SHA-256)
> **Estado:** Parcial — `SHA256SUMS` generado; falta la firma GPG y la verificación en el despliegue.
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
| `deploy/render/webgl/Build/` | Artefacto versionado en el repo | Punto de comparación contra el build local |

Los hashes se generan con:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath <archivo>
```

---

## 4. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando | `Get-FileHash -Algorithm SHA256 -LiteralPath deploy\render\webgl\Build\<archivo>` |
| Resultado esperado | Coincide con `SHA256SUMS` |
| Resultado observado | Coincide (generado en esta pasada) |
| Fecha/hora | 2026-11-25 |

---

## 5. Limitaciones y riesgo residual

- **Sin firma GPG:** el `SHA256SUMS` actual no está firmado, así que un atacante que
  reemplace el build también podría reemplazar el archivo de hashes. El `gpg` que trae Git
  para Windows no puede generar claves (`keyboxd` no instalado): hay que instalar GnuPG y
  firmar `SHA256SUMS` con una clave de la división. Pendiente de `SEC-11`.
- **No hay verificación en el despliegue:** falta un paso en el procedimiento de publicación
  que compare el webroot contra `SHA256SUMS` (y la firma). Pendiente con FIM (A.8.15).
- **`builds/webgl` no está versionado**, por lo que la base de comparación es
  `deploy/render/webgl/Build/` (que sí lo está). Al regenerar el build hay que regenerar
  este `SHA256SUMS`.

---

## 6. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| [`SHA256SUMS`](SHA256SUMS) | Sumas SHA-256 de los 4 archivos del build |

---

## 7. Checklist de cierre

- [x] `SHA256SUMS` generado sobre el artefacto versionado.
- [ ] Firma GPG del `SHA256SUMS` (requiere GnuPG + clave de la división).
- [ ] Verificación de integridad en el procedimiento de despliegue.
- [ ] Regenerar `SHA256SUMS` tras cada release.
