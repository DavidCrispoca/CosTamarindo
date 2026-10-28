# Assets de Figma

Exportaciones fuente del archivo de diseño **CosTamarindo**
(`qI8jr7abLu6tIpL0MnL8Ol`), usadas para generar los sprites y prefabs de Unity.

## Organización

```
assets/figma/
├── tokens/       # paleta y tipografías (json/md)
├── ui/           # botones, inputs, paneles, iconos (SVG)
├── items/        # ítems que caen (PNG/SVG)
├── characters/   # Palenquera, niña alegre, niña triste
├── backgrounds/  # fondos de playa y viñeta
└── screens/      # referencias de pantalla completas
```

Los archivos que Unity importa viven en `unity/Assets/Art/`; esta carpeta conserva
el origen para poder reexportar si el diseño cambia.

## Tipografías

| Uso | Familia | Licencia |
| :--- | :--- | :--- |
| Display / logo | Lilita One | SIL Open Font License 1.1 |
| Cuerpo / UI | Patrick Hand | SIL Open Font License 1.1 |

## Nota

Estos assets son de uso exclusivo del proyecto académico. No redistribuir arte
de terceros sin revisar su licencia individual.

## Cómo reexportar (API REST, sin cuota de MCP)

```bash
# 1. Token personal con scope file_content:read → .env (nunca se commitea)
#    FIGMA_TOKEN=figd_...
# 2. Inventario de nodos
node tools/figma-export/export.mjs --list
# 3. Exportar por lote (PNG @3x con transparencia)
node tools/figma-export/export.mjs --ids 1:440,1:450 --out ../../assets/figma/items
```

Los PNG resultantes se copian a `unity/Assets/Art/` y se importan como Sprite
(PPU 100, máx. 512). `ItemSpawner.NormalizeScale` iguala el tamaño en pantalla.

