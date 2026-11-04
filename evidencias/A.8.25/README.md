# A.8.25 · Ciclo de vida seguro del desarrollo — evidencia

> **Control:** ISO/IEC 27001:2022 A.8.25 — *Secure development life cycle*
> **Fecha de ejecución:** 2026-11-03
> **Herramienta:** Gitleaks 8.30.1
> **Estado:** ✅ implementado y verificado

---

## 1. Qué se controla

Que **ningún secreto** (clave de API, token, contraseña, clave privada) entre al
repositorio, ni en un commit nuevo ni en los 35 commits ya existentes del historial.

La amenaza concreta: un `.env`, un token de Supabase o una credencial pegada en un script
que termina versionada y visible para cualquiera con acceso al repositorio. Una vez en el
historial, borrarla no alcanza: queda en los commits anteriores y hay que reescribir la
historia o rotar la credencial.

---

## 2. Implementación

| Pieza | Ruta | Función |
| :--- | :--- | :--- |
| Configuración | `.gitleaks.toml` | Extiende las 150+ reglas por defecto y añade una allowlist justificada |
| Hook versionado | `tools/git-hooks/pre-commit` | Escanea el índice de Git antes de cada commit |
| Instalador | `tools/git-hooks/install.ps1` | Enlaza el hook en `.git/hooks` y hornea la ruta de gitleaks |
| Evidencia | `evidencias/A.8.25/*.json` | Escaneos antes/después y ataque bloqueado |

### Instalación en una máquina nueva

```powershell
winget install Gitleaks.Gitleaks
powershell -File tools/git-hooks/install.ps1
```

---

## 3. Hallazgo 1 · línea base del historial

Escaneados **35 commits** (~8,24 MB) con las reglas por defecto.

| Campo | Valor |
| :--- | :--- |
| Regla | `generic-api-key` |
| Archivo | `docs/fases/F1.md:112` |
| Commit | `9993cbc` |
| Valor | `qI8jr7abLu6tIpL0MnL8Ol` |
| Entropía | 4,06 |

**Análisis.** No es una credencial. Es el `fileKey` del archivo de diseño en Figma: un
identificador público del documento. El acceso al archivo lo controla Figma por permisos de
equipo, no por esa cadena, y la URL pública ya estaba documentada en el mismo repositorio.
Rotarla no tiene sentido porque no hay nada que rotar.

**Corrección.** Se añade a la allowlist **el valor exacto**, no el patrón. Un token real de
Figma (`figd_...`) sigue disparando la regla. La razón queda escrita en `.gitleaks.toml`.

**Resultado:** historial completo, 0 hallazgos (`02-history-after.json`).

---

## 4. Hallazgo 2 · prueba de ataque

Se preparó en el índice de Git un archivo con un token inventado con formato real
(`ghp_` + 36 caracteres), sin commitear.

```
[pre-commit] gitleaks: escaneando cambios preparados...
gitleaks: leaks found: 1
[pre-commit] COMMIT BLOQUEADO: gitleaks detecto un posible secreto.
[pre-commit] Nunca usar 'git commit --no-verify' para saltarlo.
[pre-commit] exit code: 1
```

**Resultado:** el commit se bloquea con código de salida **1**. El archivo de prueba se
retiró del índice después; no forma parte del historial.

Evidencia: `03-attack-blocked.json` (el reporte del escaneo que originó el bloqueo).

---

## 5. Caso de control · archivo limpio

Para descartar falsos positivos sistemáticos (un hook que bloquea todo es tan inútil como
uno que no bloquea nada), se verificó que un archivo sin secretos pasa:

```
[pre-commit] gitleaks: sin hallazgos.
[pre-commit] exit code: 0
```

---

## 6. Un fallo real que se encontró y se corrigió

La primera versión del hook usaba `gitleaks` a secas y dependía del `PATH`. Al ejecutarlo
desde el `sh` de Git para Windows, **la herramienta no estaba en PATH** aunque Windows ya la
tuviera instalada: el hook avisaba y **dejaba pasar el commit** con código 0.

Ese es el peor modo de fallo posible en un control de seguridad: parece implementado y no
protege nada. La corrección fue que `install.ps1` resuelva la ruta absoluta del binario y la
escriba dentro del hook instalado (`GITLEAKS="/c/Users/.../gitleaks.exe"`), convirtiendo la
ruta de Windows a formato POSIX. Se verificó ejecutando el hook como lo hace Git y
comprobando el código de salida en los dos casos.

**Lección:** un control de seguridad se verifica ejecutándolo, no leyendo su configuración.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| `01-history-before.json` | Historial con reglas por defecto: 1 hallazgo (el `fileKey`) |
| `02-history-after.json` | Historial con la allowlist: 0 hallazgos (`[]`) |
| `03-attack-blocked.json` | Escaneo del índice con el token falso: 1 hallazgo, bloqueo |

---

## 8. Escaneo del árbol de trabajo: por qué da 90 y no cero

`gitleaks dir .` sobre la carpeta completa reporta **90 hallazgos**, y eso **no es un fallo**:

| Origen | Cantidad | Veredicto |
| :--- | ---: | :--- |
| `unity/Library/Bee/artifacts/.../il2cppOutput/*.cpp` | 88 | Código C++ **generado** por IL2CPP a partir de los paquetes de Unity. Falsos positivos de `generic-api-key`. |
| `unity/Library/PackageCache/**` | 1 | Código de paquetes de Unity descargados. Falso positivo. |
| `infra/tls/juego.local-key.pem` | 1 | **Clave privada TLS local real** — y está bien detectada |

Las dos primeras categorías son artefactos de build: `unity/Library/` está en `.gitignore` y
**no hay ningún archivo de `Library/` en el índice de Git** (verificado con `git ls-files`).

El hallazgo de `infra/tls/` es el comportamiento **correcto**: la clave privada del certificado
de `juego.local` existe en disco para que Nginx firme TLS, y Gitleaks la marca como tal. Lo
importante es que `infra/tls/` está en `.gitignore` (línea 127) y el archivo **no** está
versionado, así que el hook bloquearía cualquier intento de commitearlo.

### Conclusión operativa

El escaneo **autoritativo** es el del historial de Git y el del índice (`--staged`), porque eso
es exactamente lo que se publica. Sobre el árbol de trabajo, `gitleaks dir` mezcla artefactos
generados y por eso no se usa como puerta de calidad.

No se añadió `Library/` a la allowlist a propósito: una allowlist por ruta también silenciaría
el caso de alguien forzando `git add -f` sobre un archivo de `Library/`. Es preferible que el
escaneo del árbol haga ruido y que la puerta real (historial + pre-commit) siga siendo estricta.

### Verificación

```bash
git check-ignore -v infra/tls/juego.local-key.pem
# .gitignore:127:infra/tls/   infra/tls/juego.local-key.pem

git ls-files | grep -E '^unity/Library/|\.pem$'
# (sin resultados)
```

---

## 9. Pendiente de este control

- [ ] Ejecutar Gitleaks también en CI (GitHub Actions), no solo en local.
- [ ] Repetir el escaneo de historial tras cada release.
