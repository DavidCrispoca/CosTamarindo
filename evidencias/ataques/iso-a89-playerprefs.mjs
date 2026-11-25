// A.8.9 · inspección y manipulación del almacenamiento del cliente (PlayerPrefs)
// Demuestra que PlayerPrefs solo guardan datos locales (récord + colección) y que
// editarlos no cambia el leaderboard del servidor (el servidor es autoritativo).
import { chromium } from 'file:///C:/Users/USUARIO/Desktop/CosTamarindo/tools/dev-server/node_modules/playwright-core/index.mjs'

const URL = process.argv[2] ?? 'https://costamarindo.onrender.com'

const browser = await chromium.launch({
  channel: 'msedge',
  headless: true,
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--allow-file-access-from-files'],
})
const page = await browser.newPage({ viewport: { width: 1280, height: 720 } })

const apiCalls = []
page.on('response', (r) => { if (r.url().includes('/api/')) apiCalls.push(`${r.status()} ${r.request().method()} ${r.url()}`) })

await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 90000 })
await page.waitForFunction(() => document.querySelector('#unity-loading-bar')?.style.display === 'none', null, { timeout: 120000 }).catch(() => {})
await page.waitForTimeout(3000)

// 1) localStorage
const localStorage = await page.evaluate(() => {
  const out = {}
  for (let i = 0; i < window.localStorage.length; i++) {
    const k = window.localStorage.key(i)
    out[k] = window.localStorage.getItem(k)
  }
  return out
})

// 2) IndexedDB (bases y object stores)
const idb = await page.evaluate(async () => {
  const dbs = await indexedDB.databases()
  const result = []
  for (const db of dbs) {
    const entry = { name: db.name, version: db.version, stores: [] }
    try {
      const conn = await new Promise((res, rej) => {
        const req = indexedDB.open(db.name)
        req.onsuccess = () => res(req.result)
        req.onerror = () => rej(req.error)
      })
      entry.stores = Array.from(conn.objectStoreNames)
      conn.close()
    } catch (e) { entry.stores = ['error:' + e.message] }
    result.push(entry)
  }
  return result
})

// 3) leer claves de PlayerPrefs dentro de IndexedDB (Unity usa /idbfs -> FILE_DATA)
const prefs = await page.evaluate(async () => {
  const found = []
  for (const db of await indexedDB.databases()) {
    const conn = await new Promise((res, rej) => {
      const req = indexedDB.open(db.name)
      req.onsuccess = () => res(req.result)
      req.onerror = () => rej(req.error)
    })
    for (const store of Array.from(conn.objectStoreNames)) {
      try {
        const tx = conn.transaction(store, 'readonly')
        const req = tx.objectStore(store).getAllKeys()
        const keys = await new Promise((res) => { req.onsuccess = () => res(req.result) })
        for (const k of keys) found.push({ db: db.name, store, key: String(k) })
      } catch (e) {}
    }
    conn.close()
  }
  return found
})

// 4) intentar inyectar un récord falso en IndexedDB (no debe afectar al servidor)
const injected = await page.evaluate(async () => {
  try {
    for (const db of await indexedDB.databases()) {
      const conn = await new Promise((res) => {
        const req = indexedDB.open(db.name)
        req.onsuccess = () => res(req.result)
        req.onerror = () => res(null)
      })
      if (!conn) continue
      for (const store of Array.from(conn.objectStoreNames)) {
        try {
          const tx = conn.transaction(store, 'readwrite')
          tx.objectStore(store).put('999999', 'BestScore')
          await new Promise((res) => { tx.oncomplete = res; tx.onerror = res })
        } catch (e) {}
      }
      conn.close()
    }
    return true
  } catch (e) { return 'error:' + e.message }
})

console.log(JSON.stringify({ localStorage, idb, prefs, injected, apiCalls: [...new Set(apiCalls)] }, null, 2))
await browser.close()
