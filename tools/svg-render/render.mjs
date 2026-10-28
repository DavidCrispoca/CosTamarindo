/**
 * Renderiza los SVG exportados de Figma a PNG con canal alfa.
 *
 * Uso:
 *   npm install
 *   node render.mjs <carpeta-svg> <carpeta-salida> [escala]
 *
 * Por qué existe: la exportación PNG del MCP de Figma aplana el fondo y pierde
 * la transparencia (el badge crema del ítem queda sobre blanco). Partir del SVG
 * conserva el alfa y evita depender de la cuota del MCP.
 */
import { Resvg } from "@resvg/resvg-js";
import { readFileSync, writeFileSync, readdirSync, mkdirSync } from "node:fs";
import { basename, join } from "node:path";

const [srcDir, outDir, scaleArg] = process.argv.slice(2);

if (!srcDir || !outDir) {
  console.error("Uso: node render.mjs <carpeta-svg> <carpeta-salida> [escala]");
  process.exit(1);
}

const scale = Number(scaleArg || 3);
mkdirSync(outDir, { recursive: true });

const files = readdirSync(srcDir).filter((f) => f.toLowerCase().endsWith(".svg"));
if (files.length === 0) {
  console.error(`No se encontraron SVG en ${srcDir}`);
  process.exit(1);
}

for (const file of files) {
  const svg = readFileSync(join(srcDir, file), "utf8");
  const resvg = new Resvg(svg, {
    fitTo: { mode: "zoom", value: scale },
    background: "rgba(0,0,0,0)",
  });

  const png = resvg.render().asPng();
  const out = join(outDir, basename(file, ".svg") + ".png");
  writeFileSync(out, png);
  console.log(`${file} -> ${out} (${png.length} bytes)`);
}
