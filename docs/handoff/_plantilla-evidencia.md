# Plantilla de evidencia · control ISO/IEC 27001:2022

> Copiar esta estructura en `evidencias/<control>/README.md`, por ejemplo
> `evidencias/A.8.24/README.md`.
>
> **Regla:** la evidencia se produce **ejecutando** el ataque. Un control se verifica
> ejecutándolo, no leyendo su configuración.

---

# <Control> · <Nombre del control> — evidencia

> **Control:** ISO/IEC 27001:2022 <A.8.xx> — *<nombre en inglés>*
> **División:** <Frontend | Backend | DB | Security | IA>
> **Fecha de ejecución:** AAAA-MM-DD
> **Commit de la evidencia:** `<hash>`
> **Herramienta:** <Burp Suite | curl | nmap | ZAP | Gitleaks | Wazuh…>
> **Estado:** <✅ implementado y verificado | 🚧 parcial | ⬜ pendiente>

---

## 1. Qué se controla

Una o dos frases. Qué amenaza concreta evita y sobre qué activo.

---

## 2. Estado vulnerable (antes)

Cómo estaba el sistema antes de la corrección. Si el control nació implementado, decirlo y
justificar por qué (por ejemplo: «se implementó desde el primer commit»).

| Elemento | Valor |
| :--- | :--- |
| Punto de entrada | `POST /api/score` |
| Configuración previa | … |
| Resultado del ataque | 200 con puntaje alterado |

---

## 3. Ataque reproducible

Comando **literal** que reproduce el ataque, sin secretos reales (usar `<token>`,
`<session_id>`).

```powershell
# comando
```

Salida literal:

```
# salida
```

Captura (si aplica): `ataque.png`

---

## 4. Corrección

Qué se cambió, en qué archivo y por qué. Enlazar el commit.

| Archivo | Cambio |
| :--- | :--- |
| `backend/app/...` | … |

---

## 5. Verificación (después)

Mismo ataque, ahora rechazado.

```
HTTP 400 {"code":"bad_signature"}
```

Y el registro de auditoría correspondiente:

```json
{"event":"score_rejected","reason":"bad_signature","user_id":"…","ip":"172.18.0.0"}
```

---

## 6. Qué demuestra y qué no

Sé explícito con el límite. Ejemplo: «el HMAC eleva el coste y evita manipulación en
tránsito, pero no impide que un cliente comprometido firme un puntaje; la defensa real es la
plausibilidad en servidor».

---

## 7. Artefactos

| Archivo | Qué muestra |
| :--- | :--- |
| `ataque.json` | salida de la herramienta |
| `ataque.png` | captura |
| `verificacion.log` | log del rechazo |

---

## 8. Pendiente de este control

- [ ] …

---

### Checklist antes de dar el control por cerrado

- [ ] Hay un «antes» y un «después» verificables.
- [ ] El comando del ataque se puede volver a ejecutar tal cual.
- [ ] La salida pegada es literal, no resumida.
- [ ] No hay claves, tokens completos ni IPs reales sin enmascarar.
- [ ] El commit está citado.
- [ ] La entrada está enlazada desde `Plan.md` §7.
