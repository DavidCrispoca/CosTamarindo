# Seguridad del repositorio

Este repositorio es **público**. Antes de abrir un *pull request* o hacer *push* a `main`,
lee la política completa: **[`docs/seguridad/politica-secretos.md`](docs/seguridad/politica-secretos.md)**.

## Regla de oro

> **Nunca subas claves, tokens, contraseñas, cadenas de conexión, certificados privados ni
> ningún dato sensible a este repositorio.** Ni en un commit, ni en un issue, ni en un
> comentario, ni en una captura de pantalla, ni en el chat del agente.

El repositorio solo versiona plantillas (`*.example`). Los valores reales viven en el
`.env` local o en el gestor de secretos del despliegue, nunca en Git.

## Qué hacer si se filtró un secreto

1. **Rótalo de inmediato** en el proveedor (Supabase, JWT, Figma, etc.). Asume que ya es público.
2. **No lo borres con un commit nuevo**: el historial conserva el valor. Hay que reescribir el
   historial o, preferiblemente, invalidar el secreto.
3. Abre un aviso privado al responsable de `Sg-Security` y registra el incidente.

## Comprobaciones obligatorias antes de commitear

```powershell
gitleaks git --redact --verbose        # historial completo
gitleaks protect --staged --redact     # cambios en el índice
git status --short                     # ¿aparece .env, *.pem, builds/, Library/…?
```

El CI ejecuta Gitleaks en cada push y pull request
([`.github/workflows/security.yml`](.github/workflows/security.yml)). Un fallo del escaneo
bloquea la integración.
