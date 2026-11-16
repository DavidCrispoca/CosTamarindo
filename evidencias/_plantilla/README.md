# Plantilla de evidencia — control ISO/IEC 27001:2022

> Copiar esta carpeta a `evidencias/<control>/` y completar todos los campos.
> Una evidencia sin ataque reproducible no cierra el control.
>
> **Control:** A.8.XX — *nombre del control*
> **Fecha de ejecución:** AAAA-MM-DD
> **Responsable:** división Security
> **Estado:** Implementado / Parcial / Pendiente
> **Riesgo asociado:** R-XX (ver `docs/soa.md` §5)

---

## 1. Qué se controla

Explicar en 3–5 líneas el activo, la amenaza y el resultado esperado. No describir la
herramienta todavía: primero el riesgo y la propiedad de seguridad que se quiere demostrar
(confidencialidad, integridad, disponibilidad, autenticidad, trazabilidad).

---

## 2. Antes: estado vulnerable o línea base

| Campo | Valor |
| :--- | :--- |
| Entorno | Host / VM / contenedor / navegador |
| Versión | |
| Configuración relevante | |
| Comando de línea base | |
| Resultado observado | |

Adjuntar salida literal en un archivo `01-linea-base.txt` o similar. No pegar capturas sin
texto reproducible.

---

## 3. Ataque reproducible

| Campo | Valor |
| :--- | :--- |
| Herramienta | |
| Comando exacto | |
| Precondiciones | |
| Resultado esperado si el control falla | |

```bash
# comando reproducible, sin secretos reales
```

Adjuntar `02-ataque.txt` con la salida literal. Si el ataque modifica datos, indicar cómo se
restaura el entorno.

---

## 4. Corrección

Explicar qué se cambió, en qué archivo y por qué. Enlazar el commit o el archivo del repo.
No pegar claves, tokens ni contraseñas.

| Archivo | Cambio | Motivo |
| :--- | :--- | :--- |
| | | |

---

## 5. Verificación posterior

| Campo | Valor |
| :--- | :--- |
| Comando de verificación | |
| Resultado esperado | |
| Resultado observado | |
| Fecha/hora | |

Adjuntar `03-verificacion.txt`. La verificación debe ejecutarse **después** de la corrección y
sobre el mismo entorno, no sobre una maqueta distinta.

---

## 6. Limitaciones y riesgo residual

- Qué no cubre esta evidencia.
- Qué depende de un tercero o de una fase posterior.
- Qué riesgo residual se acepta y con qué compensación.

---

## 7. Artefactos

| Archivo | Qué demuestra |
| :--- | :--- |
| `01-linea-base.txt` | Estado antes del ataque |
| `02-ataque.txt` | Ataque reproducible y resultado |
| `03-verificacion.txt` | Corrección verificada |
| | |

---

## 8. Checklist de cierre

- [ ] El ataque se ejecutó de verdad y la salida está adjunta.
- [ ] La corrección está en el repositorio y es reproducible.
- [ ] La verificación posterior demuestra el cambio.
- [ ] No hay secretos reales en la evidencia.
- [ ] La limitación y el riesgo residual están escritos.
- [ ] `docs/soa.md` apunta a esta carpeta.
