// Verificación del build WebGL en navegador real (Edge/Chromium) sin descargar
// navegadores: usa `playwright-core` + el Edge instalado en Windows.
//
// Uso:  node tools/dev-server/check.mjs [url] [ruta-screenshot]
// Requiere el servidor levantado (node tools/dev-server/server.mjs 8080).

import { chromium } from 'playwright-core'

const URL_TARGET = process.argv[2] ?? 'http://localhost:8080/'
const SHOT = process.argv[3] ?? '../../evidencias/F3/screenshot-game.png'

const browser = await chromium.launch({
  channel: 'msedge',
  headless: true,
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--allow-file-access-from-files'],
})

const page = await browser.newPage({ viewport: { width: 1280, height: 720 } })

const errors = []
const warnings = []
page.on('console', msg => {
  if (msg.type() === 'error') errors.push(msg.text())
  if (msg.type() === 'warning') warnings.push(msg.text())
})
page.on('pageerror', err => errors.push(`pageerror: ${err.message}`))
page.on('requestfailed', req => errors.push(`requestfailed: ${req.url()} ${req.failure()?.errorText}`))

const started = Date.now()
const responses = []
page.on('response', res => {
  const length = Number(res.headers()['content-length'] ?? 0)
  responses.push({ url: res.url(), status: res.status(), bytes: Number.isFinite(length) ? length : 0 })
})

await page.goto(URL_TARGET, { waitUntil: 'domcontentloaded', timeout: 60000 })

let ready = false
let loadSeconds = null
try {
  // El template de Unity oculta la barra de carga cuando el runtime arranca.
  await page.waitForFunction(
    () => document.querySelector('#unity-loading-bar')?.style.display === 'none',
    null,
    { timeout: 90000 },
  )
  loadSeconds = ((Date.now() - started) / 1000).toFixed(1)
  ready = true
} catch { /* se informa abajo */ }

// Deja correr el bucle unos segundos para detectar errores de runtime.
await page.waitForTimeout(4000)

// Movimiento con teclado dentro del canvas (jugabilidad mínima).
await page.mouse.click(640, 360)
await page.keyboard.down('ArrowRight')
await page.waitForTimeout(700)
await page.keyboard.up('ArrowRight')

const canvas = await page.evaluate(() => {
  const c = document.querySelector('canvas')
  return c ? { width: c.width, height: c.height } : null
})

const seconds = ((Date.now() - started) / 1000).toFixed(1)
await page.screenshot({ path: SHOT, fullPage: false })
const totalBytes = responses.reduce((sum, r) => sum + r.bytes, 0)
await browser.close()

const bad = responses.filter(r => r.status >= 400).map(r => `${r.status} ${r.url}`)

console.log(JSON.stringify({
  url: URL_TARGET,
  unityReady: ready,
  loadSeconds,
  secondsTotal: seconds,
  transferMB: +(totalBytes / 1048576).toFixed(1),
  canvas,
  httpErrors: bad,
  consoleErrors: errors,
  consoleWarnings: warnings.slice(0, 5),
  screenshot: SHOT,
}, null, 2))

process.exit(ready && errors.length === 0 && bad.length === 0 ? 0 : 1)
