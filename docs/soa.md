# Declaración de Aplicabilidad (SoA) — CosTamarindo 2D

> **Norma:** ISO/IEC 27001:2022, Anexo A (93 controles)
> **Alcance:** proyecto académico CosTamarindo 2D y sus cinco nodos: `Sg-Frontend`,
> `Sg-Backend`, `Sg-DB`, `Sg-AI` y `Sg-Security`.
> **Fecha de evaluación:** 2026-11-13
> **Responsable:** división Security (`Sg-Security`)
> **Estado:** evaluación inicial completa; los controles aplicables se cierran con evidencia
> en `evidencias/`.

---

## 1. Cómo leer este documento

La columna **Aplicabilidad** responde solo a la pregunta *¿el control entra en el alcance del
proyecto?*:

| Valor | Significado |
| :--- | :--- |
| **Aplica** | El control es relevante para el proyecto y debe tener decisión, implementación o evidencia. |
| **No aplica** | El control no es relevante por el contexto del proyecto (por ejemplo, no hay empleados ni oficinas). La exclusión está justificada. |
| **Fuera de alcance** | El control corresponde a un proveedor o a una obligación que el proyecto hereda y no opera directamente (por ejemplo, Supabase Cloud). |

La columna **Estado** describe el avance real y no se deduce de la aplicabilidad:

| Valor | Significado |
| :--- | :--- |
| **Implementado** | Existe una medida verificable y evidencia en el repositorio. |
| **Parcial** | Hay una medida o documentación inicial, pero falta ataque, verificación o alcance completo. |
| **Pendiente** | El control aplica, pero todavía no tiene medida ni evidencia. |
| **N/A** | No corresponde implementarlo por la decisión de aplicabilidad. |

> **Regla de honestidad:** un control marcado **Aplica / Pendiente** no está cumplido. El SoA
> no es una lista de deseos: cada cierre debe apuntar a una carpeta de `evidencias/` con
> ataque, corrección y verificación.

---

## 2. Contexto y exclusiones generales

CosTamarindo 2D es un trabajo académico desarrollado por una sola persona, sin empleados,
sin oficinas propias, sin clientes reales y sin datos personales de terceros. La operación
se reparte entre un equipo personal (host Docker) y una VM local de Wazuh; la base de datos
es Supabase Cloud en un proyecto de desarrollo.

Por ese contexto se excluyen, con justificación, los controles de personas y de seguridad
física. No se excluyen controles técnicos que sí afectan al juego, a los secretos, a los
puntajes, a los logs o a la red.

### 2.1 Activos principales

| Activo | Dueño | Por qué importa |
| :--- | :--- | :--- |
| Código fuente y build WebGL | Frontend / Backend | Integridad del juego y de la cadena de suministro |
| Credenciales de Supabase y JWT | Backend / DB | Acceso a datos y firma de sesiones |
| Puntajes y leaderboard | Backend / DB | Integridad del resultado competitivo |
| Logs de Nginx, backend y Wazuh | Security | Trazabilidad y respuesta a incidentes |
| VM `sg-security` y dashboard Wazuh | Security | Confidencialidad de la evidencia y del SIEM |
| Modelo y prompt de Ollama | IA | Abuso del NPC y fuga de instrucciones |

### 2.2 Criterios de exclusión

- **Personas:** no hay relación laboral, contratación, inducción, disciplina ni desvinculación.
- **Físico:** no hay oficinas, salas seguras, cableado propio ni centro de datos operado por el
  proyecto. El equipo personal y la VM se tratan como puesto de trabajo académico.
- **Proveedores:** Supabase Cloud y GitHub son servicios heredados; el proyecto verifica su
  configuración, pero no opera su infraestructura física ni su cadena de suministro interna.
- **Producción:** el despliegue es una demo académica; no hay clientes reales ni SLA.

---

## 3. Resumen de aplicabilidad

| Tema | Controles | Aplica | No aplica | Fuera de alcance |
| :--- | ---: | ---: | ---: | ---: |
| A.5 Organizacionales | 37 | 26 | 9 | 2 |
| A.6 Personas | 8 | 1 | 7 | 0 |
| A.7 Físicos | 14 | 0 | 14 | 0 |
| A.8 Tecnológicos | 34 | 31 | 3 | 0 |
| **Total** | **93** | **58** | **33** | **2** |

> Los conteos se mantienen a mano en esta tabla; si se cambia una fila, hay que actualizar el
> resumen. La fuente de verdad es la tabla de la sección 4.

---

## 4. Evaluación de los 93 controles

### 4.1 A.5 — Controles organizacionales

| ID | Control | Aplicabilidad | Estado | Justificación / evidencia |
| :--- | :--- | :--- | :--- | :--- |
| 5.1 | Políticas para la seguridad de la información | Aplica | Parcial | `Plan.md` §7 y este SoA; falta política firmada y revisión periódica. |
| 5.2 | Roles y responsabilidades de seguridad de la información | Aplica | Parcial | Roles por división en `Plan.md` §2; una sola persona concentra varios roles. |
| 5.3 | Segregación de funciones | Aplica | Parcial | No hay equipo; se compensa con revisión por evidencia y separación lógica de nodos. |
| 5.4 | Responsabilidades de la dirección | No aplica | N/A | No hay organización con dirección ni empleados. |
| 5.5 | Contacto con autoridades | No aplica | N/A | Proyecto académico sin obligación de reporte regulatorio. |
| 5.6 | Contacto con grupos de interés especial | No aplica | N/A | No hay programa formal de relaciones externas. |
| 5.7 | Inteligencia de amenazas | No aplica | N/A | No hay operación 24/7 ni suscripción a fuentes de threat intelligence. |
| 5.8 | Seguridad de la información en la gestión de proyectos | Aplica | Parcial | Fases, backlog y criterios de terminado en `Plan.md`; falta registro formal de riesgos por fase. |
| 5.9 | Inventario de información y activos asociados | Aplica | Parcial | Inventario inicial en §2.1; falta inventario versionado y revisión. |
| 5.10 | Uso aceptable de la información y activos | Aplica | Parcial | Reglas de no secretos y no publicación en `infra/README.md`; falta política de uso aceptable. |
| 5.11 | Devolución de activos | No aplica | N/A | No hay empleados ni activos entregados. |
| 5.12 | Clasificación de la información | Aplica | Parcial | Separación público/interno/secreto en `.env`, `.gitignore` y documentación; falta esquema formal. |
| 5.13 | Etiquetado de la información | Aplica | Parcial | Nombres `*.example`, `docs/` y `evidencias/`; falta etiqueta explícita por documento. |
| 5.14 | Transferencia de información | Aplica | Parcial | HTTPS y TLS local; transferencia de logs por red `siem`; Tailscale diferido. |
| 5.15 | Control de acceso | Aplica | Parcial | UFW, SSH por clave, roles de Supabase y perfiles Docker; falta revisión periódica. |
| 5.16 | Gestión de identidades | Aplica | Parcial | Supabase Auth y usuario `ubuntu` de la VM; falta ciclo de vida formal. |
| 5.17 | Información de autenticación | Aplica | Parcial | Contraseñas rotadas, secretos fuera del repo; falta gestor de secretos. |
| 5.18 | Derechos de acceso | Aplica | Parcial | Permisos de Supabase y Docker; falta matriz de accesos y revocación. |
| 5.19 | Seguridad en relaciones con proveedores | Aplica | Parcial | Supabase Cloud es proveedor crítico; se documenta responsabilidad compartida. |
| 5.20 | Seguridad en acuerdos con proveedores | No aplica | N/A | No hay contratos propios; se aceptan los términos del proveedor. |
| 5.21 | Seguridad en la cadena de suministro TIC | Fuera de alcance | N/A | La cadena interna de Supabase/GitHub no es operada por el proyecto. |
| 5.22 | Monitoreo, revisión y gestión de cambios de proveedores | Fuera de alcance | N/A | Depende de los avisos del proveedor; no hay proceso contractual propio. |
| 5.23 | Seguridad para el uso de servicios en la nube | Aplica | Parcial | Supabase DEV, RLS y claves separadas; falta revisión de configuración del proveedor. |
| 5.24 | Planificación y preparación de la gestión de incidentes | Aplica | Parcial | Reglas Wazuh y logs; falta runbook de incidentes y contacto. |
| 5.25 | Evaluación y decisión sobre eventos de seguridad | Aplica | Parcial | `100250` y reglas de negocio; falta triage formal. |
| 5.26 | Respuesta a incidentes de seguridad | Aplica | Parcial | Contención básica documentada; falta simulacro. |
| 5.27 | Aprendizaje de incidentes de seguridad | Aplica | Parcial | Hallazgos de FIM y TLS documentados; falta registro de lecciones. |
| 5.28 | Recopilación de evidencia | Aplica | Parcial | `evidencias/` y `alerts.json`; falta cadena de custodia formal. |
| 5.29 | Seguridad de la información durante la interrupción | No aplica | N/A | No hay servicio crítico con continuidad contractual. |
| 5.30 | Preparación TIC para la continuidad del negocio | No aplica | N/A | Demo académica sin BCP/DRP. |
| 5.31 | Requisitos legales, estatutarios, reglamentarios y contractuales | Aplica | Parcial | Licencias de arte y dependencias; falta revisión legal formal. |
| 5.32 | Derechos de propiedad intelectual | Aplica | Parcial | Arte y librerías con origen documentado; falta inventario de licencias. |
| 5.33 | Protección de registros | Aplica | Parcial | Logs y evidencias en el repo; falta retención y protección de integridad. |
| 5.34 | Privacidad y protección de datos personales | Aplica | Parcial | Solo username/PIN; no hay PII real; falta aviso de privacidad. |
| 5.35 | Revisión independiente de la seguridad de la información | No aplica | N/A | No hay auditor independiente en el alcance académico. |
| 5.36 | Cumplimiento de políticas, reglas y estándares | Aplica | Parcial | `docker compose config`, tests y gitleaks; falta auditoría interna periódica. |
| 5.37 | Procedimientos operativos documentados | Aplica | Parcial | `infra/README.md`, `infra/wazuh/README.md` y handoffs; falta revisión y versión. |

### 4.2 A.6 — Controles de personas

| ID | Control | Aplicabilidad | Estado | Justificación / evidencia |
| :--- | :--- | :--- | :--- | :--- |
| 6.1 | Selección de personal | No aplica | N/A | No hay empleados ni candidatos. |
| 6.2 | Términos y condiciones de empleo | No aplica | N/A | No hay relación laboral. |
| 6.3 | Concientización, educación y formación en seguridad | No aplica | N/A | Una sola persona; el aprendizaje queda en la documentación, no en un plan de formación. |
| 6.4 | Proceso disciplinario | No aplica | N/A | No hay empleados. |
| 6.5 | Responsabilidades después de la terminación o cambio de empleo | No aplica | N/A | No hay empleados. |
| 6.6 | Acuerdos de confidencialidad o no divulgación | No aplica | N/A | No hay terceros con acceso a información confidencial. |
| 6.7 | Trabajo remoto | No aplica | N/A | No hay empleados ni política de trabajo remoto. |
| 6.8 | Reporte de eventos de seguridad de la información | Aplica | Pendiente | El jugador no tiene canal formal de reporte; los eventos se detectan por logs. |

### 4.3 A.7 — Controles físicos

| ID | Control | Aplicabilidad | Estado | Justificación / evidencia |
| :--- | :--- | :--- | :--- | :--- |
| 7.1 | Perímetros de seguridad física | No aplica | N/A | No hay oficinas ni centro de datos propio. |
| 7.2 | Entrada física | No aplica | N/A | No hay instalaciones propias. |
| 7.3 | Protección de oficinas, salas e instalaciones | No aplica | N/A | No hay instalaciones propias. |
| 7.4 | Monitoreo de seguridad física | No aplica | N/A | No hay instalaciones propias. |
| 7.5 | Protección contra amenazas físicas y ambientales | No aplica | N/A | El equipo personal y la VM no se operan como sala segura. |
| 7.6 | Trabajo en áreas seguras | No aplica | N/A | No hay áreas seguras. |
| 7.7 | Escritorio limpio y pantalla limpia | No aplica | N/A | No hay puestos de trabajo de la organización. |
| 7.8 | Ubicación y protección de equipos | No aplica | N/A | Equipo personal; no hay inventario físico de la organización. |
| 7.9 | Seguridad de activos fuera de las instalaciones | No aplica | N/A | No hay activos de la organización fuera de instalaciones. |
| 7.10 | Medios de almacenamiento | No aplica | N/A | No se usan medios extraíbles para datos del juego. |
| 7.11 | Servicios de soporte | No aplica | N/A | No hay instalaciones con suministros críticos. |
| 7.12 | Seguridad del cableado | No aplica | N/A | No hay cableado propio de la organización. |
| 7.13 | Mantenimiento de equipos | No aplica | N/A | No hay mantenimiento de equipos de la organización. |
| 7.14 | Eliminación o reutilización segura de equipos | No aplica | N/A | No hay equipos de la organización al final de su vida útil. |

### 4.4 A.8 — Controles tecnológicos

| ID | Control | Aplicabilidad | Estado | Justificación / evidencia |
| :--- | :--- | :--- | :--- | :--- |
| 8.1 | Dispositivos de usuario final | Aplica | Parcial | Host Windows con Docker; falta hardening documentado del endpoint. |
| 8.2 | Derechos de acceso privilegiado | Aplica | Parcial | `ubuntu` con sudo y admin de Wazuh; falta inventario y revisión. |
| 8.3 | Restricción de acceso a la información | Aplica | Parcial | RLS de Supabase y redes Docker; falta prueba de acceso denegado. |
| 8.4 | Acceso al código fuente | Aplica | Parcial | Repo Git y hook de gitleaks; falta control de ramas y revisión. |
| 8.5 | Autenticación segura | Aplica | Parcial | JWT, PIN y bloqueo de login implementados; faltan ataques `alg:none` y expiración. |
| 8.6 | Gestión de la capacidad | Aplica | Parcial | Límites de memoria en Compose; falta monitoreo de consumo. |
| 8.7 | Protección contra malware | Aplica | Parcial | Imágenes oficiales y contenedores sin privilegios; no hay antivirus. |
| 8.8 | Gestión de vulnerabilidades técnicas | Aplica | Parcial | `pip-audit`/CI en preparación; falta escaneo de imagen y SonarQube. |
| 8.9 | Gestión de la configuración | Aplica | Parcial | Compose versionado y subredes explícitas; falta baseline de host/VM. |
| 8.10 | Eliminación de información | Aplica | Parcial | `docker compose down -v` y borrado de archivos de prueba; falta retención formal. |
| 8.11 | Enmascaramiento de datos | Aplica | Parcial | Reglas Wazuh enmascaran IP en algunos eventos; falta revisión de logs. |
| 8.12 | Prevención de fuga de datos | Aplica | Pendiente | CSP y `.gitignore` ayudan; falta escaneo del build y DLP. |
| 8.13 | Copia de seguridad de la información | Aplica | Implementado | `backup.ps1`/`backup.sh` y restauración probada en `evidencias/F4/`. |
| 8.14 | Redundancia de instalaciones de procesamiento | No aplica | N/A | Demo académica de un solo host; no hay requisito de alta disponibilidad. |
| 8.15 | Registro de eventos | Aplica | Parcial | Wazuh, agente 002, `100250` y FIM; `backend.log` pendiente de claves. |
| 8.16 | Monitoreo de actividades | Aplica | Parcial | Wazuh recibe logs y alerta; falta tablero y revisión periódica. |
| 8.17 | Sincronización de relojes | Aplica | Parcial | NTP del host/VM; falta verificación documentada. |
| 8.18 | Uso de programas utilitarios privilegiados | Aplica | Parcial | `sudo` y Docker admin; falta lista blanca. |
| 8.19 | Instalación de software en sistemas operativos | Aplica | Parcial | Contenedores e imágenes fijadas; `SHA256SUMS` firmado pendiente. |
| 8.20 | Seguridad de redes | Aplica | Parcial | Compose, subredes y `nmap`; falta prueba desde otro contenedor. |
| 8.21 | Seguridad de servicios de red | Aplica | Parcial | Nginx 443, UFW y rate limit; falta revisión de puertos internos. |
| 8.22 | Segregación de redes | Aplica | Implementado | Redes `dmz`, `app`, `data`, `ai`, `mgmt`, `siem` con subredes explícitas. |
| 8.23 | Filtrado web | No aplica | N/A | El juego no ofrece navegación web ni proxy de contenido. |
| 8.24 | Uso de criptografía | Aplica | Parcial | mkcert, TLS 1.2/1.3 y cabeceras; falta cifrado en reposo y gestión de claves. |
| 8.25 | Ciclo de vida seguro del desarrollo | Aplica | Implementado | Gitleaks, hook, CI de seguridad y evidencia en `evidencias/A.8.25/` y `evidencias/SEC-13/`. |
| 8.26 | Requisitos de seguridad de aplicaciones | Aplica | Parcial | Contratos y validaciones Pydantic; falta catálogo formal de requisitos. |
| 8.27 | Principios de arquitectura e ingeniería seguras | Aplica | Parcial | Separación de nodos, mínimo privilegio y redes internas; falta threat model. |
| 8.28 | Codificación segura | Aplica | Parcial | Validación, HMAC y saneo; faltan pruebas de SQLi/XSS/prompt injection. |
| 8.29 | Pruebas de seguridad en desarrollo y aceptación | Aplica | Parcial | Tests backend y nmap; falta suite de seguridad y aceptación formal. |
| 8.30 | Desarrollo externalizado | No aplica | N/A | Todo el desarrollo es propio. |
| 8.31 | Separación de entornos de desarrollo, prueba y producción | Aplica | Parcial | Supabase DEV y variables por entorno; proyecto PROD pendiente. |
| 8.32 | Gestión de cambios | Aplica | Parcial | Git y commits; falta revisión obligatoria y changelog. |
| 8.33 | Información de prueba | Aplica | Parcial | Datos sintéticos en tests; falta política de no usar datos reales. |
| 8.34 | Protección de sistemas de información durante pruebas de auditoría | Aplica | Parcial | Pruebas locales y evidencias; falta acuerdo de alcance de auditoría. |

---

## 5. Matriz de riesgos inicial

Escala: probabilidad e impacto de 1 a 5; nivel = probabilidad × impacto. Nivel bajo ≤ 4,
medio 5–9, alto 10–14, crítico ≥ 15.

| ID | Riesgo | Activo | P | I | Nivel | Tratamiento | Control / evidencia |
| :--- | :--- | :--- | ---: | ---: | :--- | :--- | :--- |
| R-01 | Secreto commiteado en el repositorio | Código / credenciales | 3 | 5 | 15 | Mitigar | Gitleaks + hook + `.gitignore` (A.8.25) |
| R-02 | Puntaje alterado o replay aceptado | Leaderboard | 4 | 5 | 20 | Mitigar | HMAC, nonce, TTL y tests (A.8.5/A.8.28) |
| R-03 | Acceso no autorizado a la VM o dashboard | Sg-Security | 3 | 5 | 15 | Mitigar | UFW, SSH por clave, dashboard no público (A.8.2/A.8.20) |
| R-04 | Pérdida o manipulación de logs | Evidencia | 3 | 4 | 12 | Mitigar | Wazuh, FIM y retención (A.8.15/A.8.16) |
| R-05 | Intercepción o suplantación TLS | Frontend | 2 | 4 | 8 | Mitigar | mkcert, TLS 1.2/1.3, HSTS (A.8.24) |
| R-06 | Fuga de información en el build WebGL | Build | 3 | 4 | 12 | Mitigar | CSP, `strings`, revisión de build (A.8.12) |
| R-07 | SQLi, XSS o prompt injection | Backend / IA | 3 | 5 | 15 | Mitigar | Validación, saneo y pruebas (A.8.28) |
| R-08 | Dependencia comprometida o vulnerable | Cadena de suministro | 3 | 4 | 12 | Mitigar | `pip-audit`, imágenes fijadas y CI (A.8.8) |
| R-09 | Abuso o denegación por ráfaga de peticiones | API | 4 | 3 | 12 | Mitigar | Rate limit Nginx + regla 100250 (A.8.20/A.8.15) |
| R-10 | Pérdida de datos por falta de backup | Supabase | 2 | 5 | 10 | Mitigar | `pg_dump` y restauración probada (A.8.13) |

### 5.1 Riesgo residual aceptado

- **R-04:** Wazuh 4.14 all-in-one guarda el estado FIM pero no emite una alerta por archivo en
  este despliegue. Se acepta como limitación documentada y se compensa con el estado en
  `002.db`, la revisión manual y la alerta de logs. Ver `evidencias/A.8.15/README.md`.
- **R-05:** el certificado es local (`juego.local`) y la CA de mkcert solo está confiada en el
  equipo de desarrollo. Para la demo se acepta; en producción se reemplaza por un certificado
  público.

---

## 6. Controles que cierran la división Security

Los 10 controles del objeto de auditoría son:

| Control | Nombre | Evidencia |
| :--- | :--- | :--- |
| A.8.5 | Autenticación segura | `evidencias/A.8.5/` (pendiente) |
| A.8.9 | Gestión de la configuración | `evidencias/A.8.9/` (pendiente) |
| A.8.12 | Prevención de fuga de datos | `evidencias/A.8.12/` (pendiente) |
| A.8.15 | Registro de eventos | `evidencias/A.8.15/` |
| A.8.19 | Instalación de software | `evidencias/A.8.19/` (pendiente) |
| A.8.20 | Seguridad de redes | `evidencias/A.8.20/` |
| A.8.24 | Uso de criptografía | `evidencias/A.8.24/` |
| A.8.25 | Ciclo de vida seguro | `evidencias/A.8.25/` |
| A.8.28 | Codificación segura | `evidencias/A.8.28/` (pendiente) |
| A.8.31 | Separación de entornos | `evidencias/A.8.31/` (pendiente) |

---

## 7. Mantenimiento

1. Revisar el SoA al cerrar cada fase (`F5`, `F7`, `F8`, `F9`).
2. Cambiar una fila solo con justificación y, si aplica, evidencia asociada.
3. Actualizar el resumen de la sección 3 cuando cambie una aplicabilidad.
4. No marcar **Implementado** sin una carpeta de evidencia con ataque, corrección y
   verificación reproducible.
5. Revisar las exclusiones si el proyecto deja de ser académico o incorpora usuarios reales.
