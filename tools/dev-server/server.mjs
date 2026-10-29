// Servidor de desarrollo local para el build WebGL.
// Nginx no está disponible fuera de Docker, y `python -m http.server` no sirve
// los `.gz` con `Content-Encoding`, por eso Unity se queda en pantalla de carga.
// Uso:  node tools/dev-server/server.mjs [puerto]
import { createServer } from 'node:http'
import { createReadStream, readFileSync, statSync } from 'node:fs'
import { extname, join, normalize } from 'node:path'
import { fileURLToPath } from 'node:url'
import { cspFor } from './csp.mjs'

const ROOT = fileURLToPath(new URL('../../builds/webgl/', import.meta.url))
const PORT = Number(process.argv[2] ?? 8080)

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml',
}

const CSP = cspFor(readFileSync(join(ROOT, 'index.html'), 'utf8'))

/** Content-Type de un archivo ya comprimido: se decide por el nombre real. */
function gzType (path) {
  if (path.endsWith('.wasm.gz')) return 'application/wasm'
  if (path.endsWith('.js.gz')) return 'application/javascript'
  return 'application/octet-stream'
}

createServer((req, res) => {
  const url = new URL(req.url, 'http://localhost')
  let path = normalize(decodeURIComponent(url.pathname)).replace(/^([/\\])+/, '')
  if (path === '' || path.endsWith('/')) path = join(path, 'index.html')

  const file = join(ROOT, path)
  if (!file.startsWith(ROOT)) { res.writeHead(403).end('forbidden'); return }

  let stat
  try { stat = statSync(file) } catch { res.writeHead(404).end('not found'); return }
  if (stat.isDirectory()) { res.writeHead(404).end('not found'); return }

  const isGz = path.endsWith('.gz')
  const headers = {
    'Content-Type': isGz ? gzType(path) : (TYPES[extname(path)] ?? 'application/octet-stream'),
    'Content-Length': stat.size,
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'DENY',
    'Referrer-Policy': 'no-referrer',
    'Content-Security-Policy': CSP,
    'Cache-Control': path.endsWith('index.html') ? 'no-cache' : 'public, max-age=3600',
  }
  if (isGz) headers['Content-Encoding'] = 'gzip'

  res.writeHead(200, headers)
  createReadStream(file).pipe(res)
}).listen(PORT, () => {
  console.log(`build WebGL en http://localhost:${PORT} (raiz: ${ROOT})`)
})
