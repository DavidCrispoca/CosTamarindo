// A.8.31 · token de DEV contra PROD (rechazo por entorno)
// Uso: node evidencias/ataques/iso-a831-env.mjs
import crypto from 'node:crypto'

const DEV = 'https://juego.local'
const PROD = 'https://costamarindo.onrender.com'
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'

async function call(base, method, path, body, token) {
  const headers = { 'Content-Type': 'application/json' }
  if (token) headers.Authorization = `Bearer ${token}`
  const res = await fetch(base + path, { method, headers, body: body ? JSON.stringify(body) : undefined })
  const text = await res.text()
  let json = {}
  try { json = text ? JSON.parse(text) : {} } catch { json = { _raw: text } }
  return { status: res.status, body: json }
}

const b64 = { dec: (s) => Buffer.from(s, 'base64url').toString() }

const uname = 'env_' + crypto.randomBytes(4).toString('hex')
console.log('== A.8.31 · separación de entornos DEV/PROD ==')

// health de ambos entornos
const devH = await call(DEV, 'GET', '/api/health')
const prodH = await call(PROD, 'GET', '/api/health')
console.log('DEV  health:', JSON.stringify(devH.body))
console.log('PROD health:', JSON.stringify(prodH.body))

// token DEV (flujo de compatibilidad)
const reg = await call(DEV, 'POST', '/api/auth/register', { username: uname, pin: '4821' })
console.log('DEV registro:', reg.status, JSON.stringify({ code: reg.body.code }))
const token = reg.body.access_token

// payload del token DEV (muestra env)
const payload = JSON.parse(b64.dec(token.split('.')[1]))
console.log('token DEV claims:', JSON.stringify({ iss: payload.iss, env: payload.env, sub: payload.sub }))

// 1) token DEV contra PROD -> debe ser rechazado (wrong_environment / invalid_token)
const prodReject = await call(PROD, 'POST', '/api/auth/logout', undefined, token)
console.log('PROD con token DEV (logout):', prodReject.status, JSON.stringify(prodReject.body))
const rejectOk = prodReject.status === 401 && ['wrong_environment', 'invalid_token'].includes(prodReject.body.code)

// 2) token DEV contra DEV -> válido (baseline)
const devOk = await call(DEV, 'POST', '/api/auth/logout', undefined, token)
console.log('DEV con token DEV (logout):', devOk.status)
const baselineOk = devOk.status === 204

// 3) token PROD propio contra PROD -> válido (para descartar que PROD rechace todo)
const prodReg = await call(PROD, 'POST', '/api/auth/register', { username: uname, pin: '4821' })
const prodToken = prodReg.body.access_token
const prodSelf = await call(PROD, 'POST', '/api/auth/logout', undefined, prodToken)
console.log('PROD con token PROD (logout):', prodSelf.status)

console.log('RESULTADO: token DEV rechazado en PROD =', rejectOk, '| token DEV válido en DEV =', baselineOk)
process.exitCode = rejectOk && baselineOk ? 0 : 1
