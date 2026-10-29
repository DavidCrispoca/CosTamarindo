# Assets de Figma

Exportaciones fuente del archivo de diseño **CosTamarindo**
(`qI8jr7abLu6tIpL0MnL8Ol`), usadas para generar los sprites y prefabs de Unity.

## Archivo de diseño (fuente de todo el arte)

| Dato | Valor |
| :--- | :--- |
| URL | <https://www.figma.com/design/qI8jr7abLu6tIpL0MnL8Ol/CosTamarindo?node-id=0-1&t=p2hEW0NuUDtfsm9e-1> |
| `fileKey` | `qI8jr7abLu6tIpL0MnL8Ol` |
| Nodo raíz | `0:1` |
| Dueño del archivo | equipo del proyecto (plan Starter) |

**Todo el arte del juego sale de ese archivo.** Si se reexporta un asset, se guarda
aquí y luego se copia a `unity/Assets/Art/`. Los nombres de archivo y los `nodeId`
del inventario están en [`docs/fases/F1.md`](../../docs/fases/F1.md).

## Estado dentro de Unity

| Asset | Origen | Destino en Unity | Estado |
| :--- | :--- | :--- | :--- |
| Palenquera | `characters/Palenquera-Ingame.png` | `Assets/Art/Characters/palenquera.png` | ✅ en escena, animada por código |
| Fondo de playa | `backgrounds/Background-Playa.png` | `Assets/Art/Backgrounds/bg_playa.png` | ✅ en escena (`BackgroundFitter`) |
| 4 ítems | `items/*.png` | `Assets/Art/Items/` | ✅ |
| 4 ítems restantes | `items/*.png` | — | ⛔ falta exportar |
| Tipografías | Google Fonts (OFL) | `Assets/Art/Fonts/` | ✅ SDF |

Los ajustes de importación no se tocan a mano: los aplica
`Assets/Editor/FigmaArtImporter.cs` (menú **CosTamarindo → Importar arte de Figma**).

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

