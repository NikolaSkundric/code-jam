// Load test of the web chat (TASK.md section 3):
//   - 300 simultaneous users, all connected over SignalR
//   - a room with 300 members: every message must reach every member in under 3 seconds
//   - personal chats between friends, and a burst of messages from many senders at once
//   - presence: a friend must see someone go online / offline in under 2 seconds
//   - a room with 10,000 messages must stay usable (newest page, paging back through everything)
//
//   docker compose --profile loadtest run --rm webloadtest
//
// Everything runs through the public API like a browser would; the test accounts are deleted at the end.
import * as signalR from '@microsoft/signalr'

const env = (name, fallback) => process.env[name] ?? fallback
const API = env('API_URL', 'http://api:8080')
const USERS = Number(env('USERS', 300))
const HISTORY_MESSAGES = Number(env('HISTORY_MESSAGES', 10000))
const PRESENCE_SAMPLES = Number(env('PRESENCE_SAMPLES', 30))
const BURST_SENDERS = Number(env('BURST_SENDERS', 100))
const DELIVERY_LIMIT_MS = 3000 // TASK.md 3.2
const PRESENCE_LIMIT_MS = 2000 // TASK.md 3.2

const run = Math.random().toString(36).slice(2, 7)
const password = 'WebLoad!' + run
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const log = (...args) => console.log(new Date().toISOString().slice(11, 19), ...args)

/** Runs tasks with limited parallelism. */
async function pool(items, limit, task) {
  const results = new Array(items.length)
  let next = 0
  await Promise.all(
    Array.from({ length: Math.min(limit, items.length) }, async () => {
      while (next < items.length) {
        const index = next++
        results[index] = await task(items[index], index)
      }
    }),
  )
  return results
}

function stats(values) {
  const sorted = [...values].sort((a, b) => a - b)
  const at = (p) => (sorted.length ? sorted[Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1)] : 0)
  return { n: sorted.length, p50: at(50), p95: at(95), p99: at(99), max: sorted.at(-1) ?? 0 }
}
const row = (label, s, unit = 'ms') =>
  console.log(`  ${label.padEnd(44)} n=${String(s.n).padStart(6)}  p50 ${String(s.p50).padStart(5)}  p95 ${String(s.p95).padStart(5)}  p99 ${String(s.p99).padStart(5)}  max ${String(s.max).padStart(5)} ${unit}`)

async function api(path, { method = 'GET', cookie, body } = {}) {
  const headers = { 'X-Requested-With': 'web-loadtest' }
  if (cookie) headers.Cookie = cookie
  if (body) headers['Content-Type'] = 'application/json'
  const started = performance.now()
  const response = await fetch(API + path, { method, headers, body: body ? JSON.stringify(body) : undefined })
  const text = await response.text()
  return {
    status: response.status,
    ms: Math.round(performance.now() - started),
    json: text ? JSON.parse(text) : null,
    cookie: (response.headers.getSetCookie?.() ?? []).map((c) => c.split(';')[0]).join('; '),
  }
}

// ------------------------------------------------------------------ SignalR clients

const deliveries = [] // { id, ms }
const deliveredCount = new Map() // message id -> how many clients received it
const presenceEvents = new Map() // receiver user index -> [{ username, status, at }]
const errors = []

function attachHandlers(user) {
  user.conn.on('MessageReceived', (message) => {
    if (!message.text?.startsWith('LT|')) return
    const [, id, sentAt] = message.text.split('|')
    deliveries.push({ id, ms: Date.now() - Number(sentAt) })
    deliveredCount.set(id, (deliveredCount.get(id) ?? 0) + 1)
  })
  user.conn.on('PresenceChanged', (change) => {
    let list = presenceEvents.get(user.index)
    if (!list) presenceEvents.set(user.index, (list = []))
    list.push({ username: change.username, status: change.status, at: Date.now() })
  })
}

async function connectUser(user) {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl(`${API}/hubs/presence`, {
      headers: { Cookie: user.cookie, 'X-Requested-With': 'web-loadtest' },
      transport: signalR.HttpTransportType.WebSockets,
      skipNegotiation: true,
    })
    .configureLogging(signalR.LogLevel.Error)
    .build()
  user.conn = conn
  attachHandlers(user)
  await conn.start()
}

// ------------------------------------------------------------------ message phases

let messageCounter = 0
/**
 * Sends the messages (user, chatId) and waits until every recipient got every one of them.
 * recipients: number of clients that must receive each message (room members, or 2 for a personal chat).
 */
async function sendPhase(label, sends, recipients, { concurrency = 50, spacingMs = 0 } = {}) {
  const ids = []
  const sendTimes = []
  const startedAt = deliveries.length
  await pool(sends, concurrency, async ({ user, chatId }) => {
    const id = `${label}-${messageCounter++}`
    ids.push(id)
    const sentAt = Date.now()
    const response = await api(`/api/chats/${chatId}/messages`, { method: 'POST', cookie: user.cookie, body: { text: `LT|${id}|${sentAt}` } })
    if (response.status !== 201) errors.push(`${label}: send returned ${response.status}`)
    sendTimes.push(response.ms)
    if (spacingMs) await sleep(spacingMs)
  })

  const expected = ids.length * recipients
  const deadline = Date.now() + 30000
  const received = () => ids.reduce((sum, id) => sum + (deliveredCount.get(id) ?? 0), 0)
  while (received() < expected && Date.now() < deadline) await sleep(100)

  const mine = deliveries.slice(startedAt).filter((d) => ids.includes(d.id))
  const lost = expected - received()
  console.log(`\n${label}: ${ids.length} messages x ${recipients} recipients = ${expected} deliveries, ${lost} missing`)
  row('HTTP send (accepted by the server)', stats(sendTimes))
  row('delivery to the recipients over SignalR', stats(mine.map((d) => d.ms)))
  return { lost, max: Math.max(0, ...mine.map((d) => d.ms)), p95: stats(mine.map((d) => d.ms)).p95 }
}

// ------------------------------------------------------------------ main

const users = Array.from({ length: USERS }, (_, index) => ({ index, username: `wl${run}u${index}` }))
const verdicts = []
const verdict = (name, ok, detail) => {
  verdicts.push(ok)
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? '  ' + detail : ''}`)
}
let historyUser
let exitCode = 0

try {
  log(`Web chat load test ${run}: ${USERS} users, history of ${HISTORY_MESSAGES} messages`)

  // ---- accounts
  log('Registering users ...')
  const registerStarted = Date.now()
  await pool(users, 12, async (user) => {
    const response = await api('/api/auth/register', {
      method: 'POST',
      body: { email: `${user.username}@webload.example`, username: user.username, password },
    })
    if (response.status !== 201) throw new Error(`register ${user.username}: HTTP ${response.status}`)
    user.id = response.json.id
    user.cookie = response.cookie
  })
  historyUser = { username: `wl${run}hist`, email: `wl${run}hist@webload.example` }
  const histRegister = await api('/api/auth/register', { method: 'POST', body: { email: historyUser.email, username: historyUser.username, password } })
  historyUser.cookie = histRegister.cookie
  log(`  ${USERS + 1} accounts in ${((Date.now() - registerStarted) / 1000).toFixed(1)} s`)

  // ---- a public room with every user in it
  const owner = users[0]
  const bigRoom = (await api('/api/rooms', { method: 'POST', cookie: owner.cookie, body: { name: `Load room ${run}`, description: 'load test', visibility: 'public' } })).json
  await pool(users.slice(1), 20, (user) => api(`/api/rooms/${bigRoom.id}/join`, { method: 'POST', cookie: user.cookie }))
  const members = (await api(`/api/rooms/${bigRoom.id}`, { cookie: owner.cookie })).json.members.length
  log(`Room with ${members} members created`)

  // ---- friendships in pairs (0-1, 2-3, ...) and a personal chat for each pair
  log('Making friends and opening personal chats ...')
  const pairs = []
  for (let i = 0; i + 1 < USERS; i += 2) pairs.push([users[i], users[i + 1]])
  await pool(pairs, 12, async ([a, b]) => {
    await api('/api/friends/requests', { method: 'POST', cookie: a.cookie, body: { username: b.username } })
    const incoming = (await api('/api/friends/requests', { cookie: b.cookie })).json.incoming[0]
    await api(`/api/friends/requests/${incoming.id}/accept`, { method: 'POST', cookie: b.cookie })
    const chat = (await api('/api/personal-chats', { method: 'POST', cookie: a.cookie, body: { userId: b.id } })).json
    a.partner = b
    b.partner = a
    a.chatId = chat.chatId
    b.chatId = chat.chatId
  })

  // ---- everybody connects
  log(`Connecting ${USERS} SignalR clients ...`)
  const connectStarted = Date.now()
  const connectTimes = []
  await pool(users, 40, async (user) => {
    const t = Date.now()
    try {
      await connectUser(user)
      connectTimes.push(Date.now() - t)
    } catch (error) {
      errors.push(`connect ${user.username}: ${error.message}`)
    }
  })
  const online = users.filter((u) => u.conn?.state === signalR.HubConnectionState.Connected).length
  log(`  ${online}/${USERS} connected in ${((Date.now() - connectStarted) / 1000).toFixed(1)} s`)
  console.log()
  row('SignalR connect', stats(connectTimes))
  verdict(`${USERS} users connected at the same time`, online === USERS, `${online}/${USERS}`)
  await sleep(3000) // let the initial presence events settle

  // ---- presence: friends see each other go offline / online
  log('Presence latency ...')
  const sample = pairs.slice(0, PRESENCE_SAMPLES).map(([a, b]) => ({ a, b }))
  const offlineLatency = []
  const onlineLatency = []
  await Promise.all(
    sample.map(async ({ a, b }) => {
      const eventsOf = () => presenceEvents.get(b.index) ?? []
      const waitFor = async (status, since) => {
        const end = Date.now() + 8000
        while (Date.now() < end) {
          const hit = eventsOf().find((e) => e.username === a.username && e.status === status && e.at >= since)
          if (hit) return hit.at - since
          await sleep(25)
        }
        return null
      }
      const goneAt = Date.now()
      await a.conn.stop()
      const offline = await waitFor('offline', goneAt)
      if (offline !== null) offlineLatency.push(offline)
      const backAt = Date.now()
      await connectUser(a)
      const back = await waitFor('online', backAt)
      if (back !== null) onlineLatency.push(back)
    }),
  )
  console.log()
  row('friend sees "offline" after a disconnect', stats(offlineLatency))
  row('friend sees "online" after a connect', stats(onlineLatency))
  verdict(`presence changes reach friends in under ${PRESENCE_LIMIT_MS / 1000} s`,
    offlineLatency.length === sample.length && onlineLatency.length === sample.length && Math.max(...offlineLatency, ...onlineLatency) < PRESENCE_LIMIT_MS,
    `${offlineLatency.length}/${sample.length} offline and ${onlineLatency.length}/${sample.length} online events seen, max ${Math.max(0, ...offlineLatency, ...onlineLatency)} ms`)

  // ---- messages in the room with 300 members: 10 senders, 3 messages each, spread out
  const roomSenders = users.slice(0, 10)
  const roomSends = roomSenders.flatMap((user) => [1, 2, 3].map(() => ({ user, chatId: bigRoom.id })))
  const roomResult = await sendPhase('room-300', roomSends, members, { concurrency: 10, spacingMs: 300 })
  verdict(`room with ${members} members: every message delivered to everyone in under ${DELIVERY_LIMIT_MS / 1000} s`, roomResult.lost === 0 && roomResult.max < DELIVERY_LIMIT_MS, `lost ${roomResult.lost}, max ${roomResult.max} ms`)

  // ---- personal chats: both people in every pair write 5 messages
  const personalSends = pairs.flatMap(([a, b]) => [a, b].flatMap((user) => Array.from({ length: 5 }, () => ({ user, chatId: user.chatId }))))
  const personalResult = await sendPhase('personal', personalSends, 2, { concurrency: 60 })
  verdict('personal chats: delivered in under 3 s', personalResult.lost === 0 && personalResult.max < DELIVERY_LIMIT_MS, `lost ${personalResult.lost}, max ${personalResult.max} ms`)

  // ---- burst: many senders write into the big room at the same moment
  const burstSends = users.slice(0, BURST_SENDERS).map((user) => ({ user, chatId: bigRoom.id }))
  const burstResult = await sendPhase('burst', burstSends, members, { concurrency: BURST_SENDERS })
  verdict(`burst: ${BURST_SENDERS} senders at once into a room of ${members}: delivered in under 3 s`, burstResult.lost === 0 && burstResult.max < DELIVERY_LIMIT_MS, `lost ${burstResult.lost}, max ${burstResult.max} ms (p95 ${burstResult.p95} ms)`)

  // ---- 10,000 messages of history in one room
  log(`Writing ${HISTORY_MESSAGES} messages into a history room ...`)
  const histRoom = (await api('/api/rooms', { method: 'POST', cookie: historyUser.cookie, body: { name: `History ${run}`, visibility: 'private' } })).json
  const seedStarted = Date.now()
  const seedTimes = []
  await pool(Array.from({ length: HISTORY_MESSAGES }), 25, async (_, i) => {
    const r = await api(`/api/chats/${histRoom.id}/messages`, { method: 'POST', cookie: historyUser.cookie, body: { text: `History message ${i + 1}` } })
    if (r.status !== 201) errors.push(`history send ${r.status}`)
    seedTimes.push(r.ms)
  })
  log(`  ${HISTORY_MESSAGES} messages written in ${((Date.now() - seedStarted) / 1000).toFixed(1)} s`)
  console.log()
  row('send a message (room with a growing history)', stats(seedTimes))

  const newest = []
  for (let i = 0; i < 30; i++) newest.push((await api(`/api/chats/${histRoom.id}/messages?limit=30`, { cookie: historyUser.cookie })).ms)
  row('open the room: newest 30 messages', stats(newest))

  const pageTimes = []
  let before
  let seen = 0
  while (true) {
    const page = await api(`/api/chats/${histRoom.id}/messages?limit=100${before ? `&before=${before}` : ''}`, { cookie: historyUser.cookie })
    pageTimes.push(page.ms)
    seen += page.json.messages.length
    if (!page.json.hasMore) break
    before = page.json.messages[0].id
  }
  row(`scroll back through all pages (100 per page)`, stats(pageTimes))

  const roomList = []
  for (let i = 0; i < 20; i++) roomList.push((await api('/api/rooms', { cookie: historyUser.cookie })).ms)
  row('room list with unread counters', stats(roomList))

  verdict(`room with ${HISTORY_MESSAGES} messages: every message reachable by paging back`, seen === HISTORY_MESSAGES, `${seen}/${HISTORY_MESSAGES}`)
  verdict('room with 10,000 messages stays fast (newest page and every older page under 500 ms)', Math.max(...newest, ...pageTimes) < 500, `slowest ${Math.max(...newest, ...pageTimes)} ms`)

  verdict('no errors during the test', errors.length === 0, errors.length ? errors.slice(0, 3).join('; ') : '')
  exitCode = verdicts.every(Boolean) ? 0 : 1
  console.log(exitCode === 0 ? '\nALL CHECKS PASSED' : `\n${verdicts.filter((v) => !v).length} CHECK(S) FAILED`)
} catch (error) {
  exitCode = 1
  console.log('ERROR', error.message)
} finally {
  log('Cleaning up ...')
  await Promise.allSettled(users.map((u) => u.conn?.stop()))
  const everyone = [...users, ...(historyUser ? [{ ...historyUser, username: historyUser.username }] : [])]
  await pool(everyone, 10, async (user) => {
    const email = user.email ?? `${user.username}@webload.example`
    const login = await api('/api/auth/login', { method: 'POST', body: { email, password, keepSignedIn: false } })
    if (login.status === 200) await api('/api/auth/account', { method: 'DELETE', cookie: login.cookie, body: { password } })
  })
}
process.exit(exitCode)
