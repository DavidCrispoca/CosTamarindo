/**
 * Exporta nodos de Figma por la API REST (sin cuota de MCP).
 *
 * Requisitos:
 *   .env en la raíz del repo con FIGMA_TOKEN y FIGMA_FILE_KEY
 *   Token personal con scope `file_content:read`
 *
 * Uso:
 *   node export.mjs --list                     # inventario de nodos (id · tipo · nombre)
 *   node export.mjs --list --filter cocada     # filtra por nombre
 *   node export.mjs --ids 1:440,1:450 --out ../../assets/figma/items
 *   node export.mjs --ids 1:440 --format svg --out ../../assets/figma/items
 *
 * Opciones: --ids  --out  --format png|svg  --scale 3
 */
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, "../..");

function loadEnv() {
  const path = join(repoRoot, ".env");
  if (!existsSync(path)) return;
  for (const line of readFileSync(path, "utf8").split(/\r?\n/)) {
    const match = /^\s*([A-Z0-9_]+)\s*=\s*(.*)\s*$/.exec(line);
    if (!match) continue;
    const [, key, value] = match;
    if (!process.env[key]) process.env[key] = value.replace(/^["']|["']$/g, "");
  }
}

function parseArgs(argv) {
  const args = { format: "png", scale: "3" };
  for (let i = 0; i < argv.length; i++) {
    const token = argv[i];
    if (!token.startsWith("--")) continue;
    const key = token.slice(2);
    const next = argv[i + 1];
    if (next && !next.startsWith("--")) {
      args[key] = next;
      i++;
    } else {
      args[key] = true;
    }
  }
  return args;
}

const API = "https://api.figma.com/v1";

function slug(text) {
  return text
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_|_$/g, "");
}

async function api(path, token) {
  const res = await fetch(`${API}${path}`, { headers: { "X-Figma-Token": token } });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Figma ${res.status} en ${path}: ${body.slice(0, 300)}`);
  }
  return res.json();
}

function walk(node, depth, out, maxDepth) {
  out.push({ id: node.id, type: node.type, name: node.name, depth });
  if (!node.children || depth >= maxDepth) return;

  for (const child of node.children) walk(child, depth + 1, out, maxDepth);
}

async function main() {
  loadEnv();
  const args = parseArgs(process.argv.slice(2));
  const token = process.env.FIGMA_TOKEN;
  const fileKey = process.env.FIGMA_FILE_KEY || "qI8jr7abLu6tIpL0MnL8Ol";

  if (!token) {
    console.error("Falta FIGMA_TOKEN en .env (scope file_content:read).");
    process.exit(1);
  }

  if (args.list) {
    const file = await api(`/files/${fileKey}?depth=6`, token);
    const nodes = [];
    walk(file.document, 0, nodes, 6);

    const filter = typeof args.filter === "string" ? args.filter.toLowerCase() : null;
    for (const n of nodes) {
      if (filter && !n.name.toLowerCase().includes(filter)) continue;
      console.log(`${n.id}\t${"  ".repeat(n.depth)}${n.type}\t${n.name}`);
    }
    return;
  }

  if (!args.ids) {
    console.error("Indica --ids 1:440,1:450 (o usa --list para ver el inventario).");
    process.exit(1);
  }

  const ids = String(args.ids).split(",").map((s) => s.trim()).filter(Boolean);
  const outDir = args.out ? resolve(here, args.out) : join(repoRoot, "assets/figma/exports");
  mkdirSync(outDir, { recursive: true });

  // La API devuelve 400 si faltan nodos; se pide el nombre junto con la URL.
  const query = `ids=${encodeURIComponent(ids.join(","))}&format=${args.format}&scale=${args.scale}`;
  const { images, err } = await api(`/images/${fileKey}?${query}`, token);

  if (err) console.error("Figma devolvió:", err);

  const file = await api(`/files/${fileKey}?depth=6`, token);
  const index = new Map();
  const collected = [];
  walk(file.document, 0, collected, 6);
  for (const n of collected) index.set(n.id, n);

  let ok = 0;
  for (const id of ids) {
    const url = images?.[id];
    if (!url) {
      console.error(`sin URL para ${id}`);
      continue;
    }

    const res = await fetch(url);
    if (!res.ok) {
      console.error(`fallo la descarga de ${id}: HTTP ${res.status}`);
      continue;
    }

    const buffer = Buffer.from(await res.arrayBuffer());
    const name = slug(index.get(id)?.name || id);
    const path = join(outDir, `${name}.${args.format}`);
    writeFileSync(path, buffer);
    console.log(`${id} -> ${path} (${buffer.length} bytes)`);
    ok++;
  }

  console.log(`\n${ok}/${ids.length} exportados en ${outDir}`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
