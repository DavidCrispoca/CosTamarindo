# SAST — Bearer CLI — antes/después

> **Herramienta:** Bearer CLI 2.1.3 (Docker `bearer/bearer:latest-amd64`)
> **Fecha:** 2026-11-25
> **Objetivo:** código del backend (`backend/`)
> **Commit antes:** `352916f` · **Commit después:** `019b9e7`

---

## Resultado (idéntico antes y después)

```
SUCCESS
88 checks were run and no failures were detected. Great job! 👏
```

| Métrica | Antes | Después |
| :--- | :--- | :--- |
| Archivos analizados | 37 (Python) | 37 (Python) |
| Reglas | 88 | 88 |
| Fallos de seguridad | **0** | **0** |
| Tipos de dato (PII/Personal Data) | 3 (33 ocurrencias) | 3 (33 ocurrencias) |

No cambió porque la corrección (etapa 6) fue de **configuración de Nginx**, no de código Python.

**Limitación:** Bearer gratuito no analiza C#; el cliente Unity no está cubierto por este SAST (lo cubre el DAST).

---

## Archivos

| Archivo | Contenido |
| :--- | :--- |
| `bearer-antes.txt` / `bearer-antes.html` / `bearer-antes.json` | reporte "antes" |
| `bearer-despues.txt` / `bearer-despues.html` | reporte "después" |
