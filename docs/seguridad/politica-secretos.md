# Política de secretos y datos sensibles

> **División:** Security · **Nodo:** `Sg-Security`
> **Controles ISO/IEC 27001:2022:** A.8.24 (criptografía), A.8.25 (SDLC seguro),
> A.8.12 (DLP), A.8.28 (codificación segura)
> **Aplica a:** todo el repositorio, incluido su historial de Git.
> **Última revisión:** 2026-11-24

---

## 1. Prohibición

Este repositorio es **público**. Queda **prohibido** subir a Git, en cualquier forma:

- Claves de API y tokens: `FIGMA_TOKEN`, `SUPABASE_SERVICE_ROLE_KEY`, `SUPABASE_ANON_KEY`,
  `JWT_SECRET`, `SCORE_HMAC_SECRET`, tokens de GitHub, claves de Ollama o de cualquier
  proveedor de IA.
- Contraseñas, PIN reales de jugadores y credenciales de administración.
- Cadenas de conexión (`postgresql://…`, `redis://…`) con usuario y contraseña.
- Certificados y claves privadas (`*.pem`, `*.key`, `*.p12`, `*.pfx`) y el material de la CA
  local.
- Volcados de base de datos, `pg_dump`, backups, `.env` reales y archivos de configuración
  local (`opencode.json*`, `.opencode/`, configuraciones de agentes).
- Datos personales de terceros, direcciones IP internas sin enmascarar en capturas, y
  cualquier artefacto que permita reproducir una credencial.

La prohibición cubre **commits, issues, pull requests, wikis, releases, comentarios de código,
capturas de pantalla y mensajes enviados a agentes de IA**. Un secreto publicado deja de ser
secreto en el instante en que se escribe.

## 2. Dónde viven los secretos

| Entorno | Ubicación permitida |
| :--- | :--- |
| Desarrollo local | `.env` en la raíz, ignorado por Git |
| CI/CD | *Secrets* del repositorio o del entorno, nunca en el YAML |
| Servidor | Gestor de secretos del host o variables de entorno del contenedor |
| Documentación | Solo nombres de variable y valores de ejemplo (`cambia-esto`) |

El repositorio **solo** versiona [`.env.example`](../../.env.example), con valores vacíos o
placeholders. `.gitignore` excluye `.env`, `.env.*`, `*.pem`, `*.key`, `*.p12`, `*.pfx`,
`secrets/` e `.infisical/`, además de `builds/`, `Library/`, `Temp/`, `Logs/` y las
configuraciones de agentes.

## 3. Flujo obligatorio antes de commitear

1. **Revisar el índice**: `git status --short` y `git diff --cached`. Si aparece un archivo
   que no reconoces como fuente, no lo agregues.
2. **Escanear el índice**: `gitleaks protect --staged --redact`.
3. **Escanear el historial** antes de publicar una rama: `gitleaks git --redact --verbose`.
4. **Revisar capturas y documentos**: ninguna imagen o log debe mostrar tokens, correos,
   IPs internas ni cadenas de conexión.
5. Si el escaneo falla, **detente**. No existe la excepción "es solo de desarrollo".

El CI ([`.github/workflows/security.yml`](../../.github/workflows/security.yml)) repite el
escaneo y ejecuta pruebas y `pip-audit`; un fallo bloquea la integración.

## 4. Canal seguro para compartir un secreto

- **Nunca** por Git, chat, correo sin cifrar, capturas ni documentos compartidos.
- Usa el gestor de contraseñas del equipo o un enlace de un solo uso con caducidad
  (por ejemplo, el *secret sharing* del gestor acordado).
- En el handoff, indica **qué variable falta y dónde se carga**, no su valor. Ejemplo:
  «Falta `SUPABASE_SERVICE_ROLE_KEY` en el `.env` del nodo `Sg-Backend`».
- Para el entorno académico, la entrega de claves se hace por el canal que defina el
  responsable de `Sg-Security`, fuera del repositorio.

## 5. Si un secreto se filtra

1. **Rotar primero, investigar después.** Invalida el secreto en el proveedor y genera uno
   nuevo. Asume que ya fue leído.
2. **Registrar el incidente**: qué secreto, desde cuándo, en qué commits y qué alcance tuvo.
3. **Limpiar el historial** solo si el secreto ya está rotado; reescribir sin rotar no sirve
   de nada. Coordina con `Sg-Security` antes de forzar un *push*.
4. **Revisar el uso**: logs de acceso, puntajes alterados, consultas inusuales a Supabase.
5. **Añadir una regla** a `.gitleaks.toml` para que ese patrón no vuelva a pasar.

## 6. Responsabilidades

| Rol | Responsabilidad |
| :--- | :--- |
| Cualquier persona con acceso al repo | No commitear secretos; ejecutar el escaneo antes de publicar |
| `Sg-Frontend` | No incrustar tokens en el build WebGL ni en `PlayerPrefs`; token en memoria |
| `Sg-Backend` | Cargar claves solo por entorno; no loggear cabeceras ni cuerpos sensibles |
| `Sg-DB` | No versionar dumps ni cadenas de conexión; RLS sin acceso de cliente |
| `Sg-Security` | Mantener reglas, CI, rotación y respuesta a incidentes |

## 7. Verificación de esta política

- `gitleaks git --redact --verbose` sobre el historial completo: sin hallazgos.
- Solo `.env.example` está versionado; `.env`, certificados y configuraciones locales están
  ignorados.
- CI de Gitleaks activo en cada push y pull request.

Cualquier excepción a esta política debe constar por escrito y aprobada por `Sg-Security`
antes del commit.
