// CSP para el build WebGL: Unity inyecta un <script> inline en index.html.
// En vez de abrir `script-src` con 'unsafe-inline', se calcula el hash SHA-256
// de cada bloque inline y se publica en la política.
//
// Uso como CLI (después de cada build):
//   node tools/dev-server/csp.mjs ../../builds/webgl/index.html ../../infra/nginx/snippets/csp.conf

import { createHash } from 'node:crypto'
import { readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const BASE = [
  "default-src 'self'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob:",
  "font-src 'self' data:",
  "connect-src 'self'",
  "worker-src 'self' blob:",
  "object-src 'none'",
  "base-uri 'none'",
  "frame-ancestors 'none'",
]

/** Devuelve los hash CSP de los <script> inline del HTML. */
export function scriptHashes (html) {
  const hashes = []
  const re = /<script\b(?![^>]*\bsrc=)[^>]*>([\s\S]*?)<\/script>/gi
  let match
  while ((match = re.exec(html)) !== null) {
    const body = match[1]
    if (body.trim() === '') continue
    hashes.push(`'sha256-${createHash('sha256').update(body, 'utf8').digest('base64')}'`)
  }
  return hashes
}

/** Política completa para un index.html concreto. */
export function cspFor (html) {
  const scriptSrc = ["'self'", "'wasm-unsafe-eval'", ...scriptHashes(html)].join(' ')
  return [`script-src ${scriptSrc}`, ...BASE].join('; ')
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const [, , htmlPath, outPath] = process.argv
  if (!htmlPath) {
    console.error('uso: node csp.mjs <index.html> [salida.conf]')
    process.exit(2)
  }
  const csp = cspFor(readFileSync(htmlPath, 'utf8'))
  if (outPath) {
    writeFileSync(outPath, `# GENERADO por tools/dev-server/csp.mjs - no editar a mano.\nadd_header Content-Security-Policy "${csp}" always;\n`)
    console.log(`CSP escrita en ${outPath}`)
  } else {
    console.log(csp)
  }
}
