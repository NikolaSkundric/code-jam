// Federation load test: N Jabber clients on server A and N on server B message each other.
//
//   docker compose --profile loadtest run --rm loadtest
//
// 1. creates N web accounts (they are also the Jabber accounts of server A) and N accounts on server B
// 2. connects all 2N clients with the @xmpp/client library (STARTTLS, SASL PLAIN)
// 3. every client sends MESSAGES chat messages to clients of the *other* server, A -> B and B -> A,
//    so every message crosses the server-to-server federation link
// 4. waits for the deliveries and prints delivery counts and latency; exit code 1 if anything got lost
// 5. disconnects and deletes the test accounts again
import { client, xml } from '@xmpp/client'

const env = (name, fallback) => process.env[name] ?? fallback
const CLIENTS = Number(env('CLIENTS', 50)) // per server
const MESSAGES = Number(env('MESSAGES', 10)) // per client
const API_URL = env('API_URL', 'http://api:8080')
const A = { name: 'A', host: env('XMPP_A_HOST', 'xmpp-a'), port: Number(env('XMPP_A_PORT', 5222)), domain: env('DOMAIN_A', 'a.chat.test') }
const B = { name: 'B', host: env('XMPP_B_HOST', 'xmpp-b'), port: Number(env('XMPP_B_PORT', 5223)), domain: env('DOMAIN_B', 'b.chat.test') }
const B_ADMIN_API = env('B_ADMIN_API', 'http://xmpp-b:5280/api')
const DELIVERY_TIMEOUT_MS = Number(env('DELIVERY_TIMEOUT_SECONDS', 60)) * 1000
const CONNECT_CONCURRENCY = Number(env('CONNECT_CONCURRENCY', 10))

const run = Math.random().toString(36).slice(2, 7)
const password = 'LoadTest!' + run
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const log = (...args) => console.log(new Date().toISOString().slice(11, 19), ...args)

/** Runs tasks with limited parallelism. */
async function pool(items, limit, task) {
  const results = []
  let next = 0
  const workers = Array.from({ length: Math.min(limit, items.length) }, async () => {
    while (next < items.length) {
      const index = next++
      results[index] = await task(items[index], index)
    }
  })
  await Promise.all(workers)
  return results
}

function percentile(sorted, p) {
  if (sorted.length === 0) return 0
  return sorted[Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1)]
}

// ---------------------------------------------------------------- accounts

const users = (server) =>
  Array.from({ length: CLIENTS }, (_, i) => ({ server, username: `lt${run}${server.name.toLowerCase()}${i}`, password }))

async function api(path, options = {}) {
  const response = await fetch(API_URL + path, {
    ...options,
    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'loadtest', ...options.headers },
  })
  return response
}

async function createWebAccount(user) {
  const response = await api('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email: `${user.username}@loadtest.example`, username: user.username, password: user.password }),
  })
  if (!response.ok) throw new Error(`register ${user.username}: HTTP ${response.status} ${await response.text()}`)
}

async function deleteWebAccount(user) {
  const login = await api('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email: `${user.username}@loadtest.example`, password: user.password, keepSignedIn: false }),
  })
  if (!login.ok) return
  const cookie = (login.headers.getSetCookie?.() ?? []).map((c) => c.split(';')[0]).join('; ')
  await api('/api/auth/account', { method: 'DELETE', headers: { Cookie: cookie }, body: JSON.stringify({ password: user.password }) })
}

async function ejabberd(command, body) {
  const response = await fetch(`${B_ADMIN_API}/${command}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!response.ok) throw new Error(`ejabberd ${command}: HTTP ${response.status} ${await response.text()}`)
}

// ---------------------------------------------------------------- clients

function connect(user) {
  return new Promise((resolve, reject) => {
    const started = Date.now()
    const xmpp = client({
      service: `xmpp://${user.server.host}:${user.server.port}`,
      domain: user.server.domain,
      username: user.username,
      password: user.password,
      resource: 'lt',
    })
    user.xmpp = xmpp
    user.received = []
    xmpp.on('error', (error) => {
      if (!user.online) reject(new Error(`${user.username}@${user.server.domain}: ${error.message || error.code || error.name || 'connection error'}`))
    })
    xmpp.on('stanza', (stanza) => {
      if (!stanza.is('message')) return
      const body = stanza.getChildText('body')
      if (body?.startsWith('lt|')) user.received.push({ body, at: Date.now() })
    })
    xmpp.on('online', async (address) => {
      user.online = true
      user.jid = address.toString()
      user.connectMs = Date.now() - started
      await xmpp.send(xml('presence'))
      resolve(user)
    })
    xmpp.start().catch(reject)
  })
}

// ---------------------------------------------------------------- main

const usersA = users(A)
const usersB = users(B)
const all = [...usersA, ...usersB]
let exitCode = 0

try {
  log(`Load test ${run}: ${CLIENTS} clients on ${A.domain} and ${CLIENTS} on ${B.domain}, ${MESSAGES} messages each, across the federation`)

  log('Creating accounts ...')
  await pool(usersA, 8, createWebAccount) // server A accounts are web accounts (checked through the API)
  await pool(usersB, 20, (user) => ejabberd('register', { user: user.username, host: B.domain, password: user.password }))

  log('Connecting clients ...')
  const connectStarted = Date.now()
  const connectWithRetry = async (user) => {
    for (let attempt = 1; ; attempt++) {
      try {
        return await connect(user)
      } catch (error) {
        if (attempt >= 3) throw error
        await user.xmpp?.stop().catch(() => {})
        await sleep(500 * attempt)
      }
    }
  }
  await pool(all, CONNECT_CONCURRENCY, connectWithRetry)
  const connectTimes = all.map((u) => u.connectMs).sort((a, b) => a - b)
  log(`  ${all.filter((u) => u.online).length}/${all.length} online in ${((Date.now() - connectStarted) / 1000).toFixed(1)} s (connect p50 ${percentile(connectTimes, 50)} ms, p95 ${percentile(connectTimes, 95)} ms)`)

  // Every client sends MESSAGES messages to the other server, spread over its clients.
  const expected = new Map() // body id -> { direction, sentAt }
  const sendAll = async (senders, receivers, direction) => {
    await Promise.all(
      senders.map(async (sender, senderIndex) => {
        for (let m = 0; m < MESSAGES; m++) {
          const receiver = receivers[(senderIndex + m) % receivers.length]
          const id = `${sender.username}>${receiver.username}#${m}`
          const sentAt = Date.now()
          expected.set(id, { direction, sentAt, to: receiver })
          await sender.xmpp.send(
            xml('message', { type: 'chat', to: `${receiver.username}@${receiver.server.domain}` }, xml('body', {}, `lt|${id}|${sentAt}`)),
          )
        }
      }),
    )
  }

  log('Sending messages A -> B and B -> A ...')
  const sendStarted = Date.now()
  await Promise.all([sendAll(usersA, usersB, 'A->B'), sendAll(usersB, usersA, 'B->A')])
  log(`  ${expected.size} messages sent in ${((Date.now() - sendStarted) / 1000).toFixed(1)} s`)

  // Wait until every message arrived (or the timeout passes).
  const deadline = Date.now() + DELIVERY_TIMEOUT_MS
  const delivered = () => all.reduce((sum, u) => sum + u.received.length, 0)
  while (delivered() < expected.size && Date.now() < deadline) await sleep(250)

  const stats = { 'A->B': { sent: 0, delivered: 0, latencies: [] }, 'B->A': { sent: 0, delivered: 0, latencies: [] } }
  for (const [id, info] of expected) {
    const entry = stats[info.direction]
    entry.sent++
    const got = info.to.received.find((r) => r.body.startsWith(`lt|${id}|`))
    if (got) {
      entry.delivered++
      entry.latencies.push(got.at - info.sentAt)
    }
  }

  log('Results:')
  console.log('  direction  sent  delivered  lost   p50 ms  p95 ms  max ms')
  for (const [direction, s] of Object.entries(stats)) {
    const sorted = s.latencies.sort((a, b) => a - b)
    console.log(
      `  ${direction.padEnd(9)}  ${String(s.sent).padStart(4)}  ${String(s.delivered).padStart(9)}  ${String(s.sent - s.delivered).padStart(4)}` +
        `   ${String(percentile(sorted, 50)).padStart(6)}  ${String(percentile(sorted, 95)).padStart(6)}  ${String(sorted.at(-1) ?? 0).padStart(6)}`,
    )
  }
  // Optionally keep the clients connected so the admin dashboard can be looked at (HOLD_SECONDS=60).
  const hold = Number(env('HOLD_SECONDS', 0))
  if (hold > 0) {
    log(`Holding the connections for ${hold} s (look at the admin dashboard) ...`)
    await sleep(hold * 1000)
  }
  const lost = Object.values(stats).reduce((sum, s) => sum + (s.sent - s.delivered), 0)
  const online = all.filter((u) => u.online).length
  if (lost > 0 || online < all.length) {
    exitCode = 1
    log(`FAILED: ${lost} messages lost, ${all.length - online} clients not connected`)
  } else {
    log(`PASSED: all ${expected.size} messages crossed the federation (${online} clients online)`)
  }
} catch (error) {
  exitCode = 1
  log('ERROR', error.message)
} finally {
  log('Cleaning up ...')
  await Promise.allSettled(all.map((u) => u.xmpp?.stop()))
  if (env('KEEP_ACCOUNTS', '') !== '1') {
    await Promise.allSettled(usersA.map(deleteWebAccount))
    await Promise.allSettled(usersB.map((u) => ejabberd('unregister', { user: u.username, host: B.domain })))
  }
}
process.exit(exitCode)
