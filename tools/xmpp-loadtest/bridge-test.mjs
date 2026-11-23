// Integration test of the bridge between Jabber and the website (run inside the compose network):
//
//   docker compose --profile loadtest run --rm loadtest node bridge-test.mjs
//
// Checks: friends appear in the Jabber roster; a Jabber message between friends shows up in the web personal chat;
// a web message reaches the Jabber client (and is not duplicated); non-friends are not mirrored; Jabber sessions
// show as online / AFK / offline on the website presence (SignalR).
import { client, xml } from '@xmpp/client'
import * as signalR from '@microsoft/signalr'

const API = process.env.API_URL ?? 'http://api:8080'
const XMPP_HOST = process.env.XMPP_A_HOST ?? 'xmpp-a'
const XMPP_PORT = Number(process.env.XMPP_A_PORT ?? 5222)
const DOMAIN = process.env.DOMAIN_A ?? 'a.chat.test'

const run = Math.random().toString(36).slice(2, 7)
const password = 'Bridge!' + run
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const results = []
const check = (name, ok, detail = '') => {
  results.push(ok)
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}${detail ? '  (' + detail + ')' : ''}`)
}

async function waitFor(predicate, timeoutMs = 8000) {
  const end = Date.now() + timeoutMs
  while (Date.now() < end) {
    const value = await predicate()
    if (value) return value
    await sleep(150)
  }
  return null
}

// ------------------------------------------------------------------ web API helpers

async function api(path, { method = 'GET', body, cookie, form } = {}) {
  const headers = { 'X-Requested-With': 'bridge-test' }
  if (cookie) headers.Cookie = cookie
  if (body) headers['Content-Type'] = 'application/json'
  const response = await fetch(API + path, { method, headers, body: form ?? (body ? JSON.stringify(body) : undefined) })
  const text = await response.text()
  return { status: response.status, json: text ? JSON.parse(text) : null, cookie: (response.headers.getSetCookie?.() ?? []).map((c) => c.split(';')[0]).join('; ') }
}

async function createUser(suffix) {
  const username = `br${run}${suffix}`
  const email = `${username}@bridgetest.example`
  const register = await api('/api/auth/register', { method: 'POST', body: { email, username, password } })
  if (register.status !== 201) throw new Error(`register ${username}: ${register.status}`)
  return { username, email, id: register.json.id, cookie: register.cookie }
}

// ------------------------------------------------------------------ Jabber helpers

function connect(user) {
  return new Promise((resolve, reject) => {
    const xmpp = client({ service: `xmpp://${XMPP_HOST}:${XMPP_PORT}`, domain: DOMAIN, username: user.username.toLowerCase(), password, resource: 'bt' })
    const received = []
    xmpp.on('error', (error) => {
      if (!user.online) reject(new Error(error.message || error.code || 'connection error'))
    })
    xmpp.on('stanza', (stanza) => {
      if (stanza.is('message') && stanza.getChildText('body')) received.push({ from: stanza.attrs.from, body: stanza.getChildText('body') })
    })
    xmpp.on('online', () => {
      user.online = true
      resolve({ xmpp, received })
    })
    xmpp.start().catch(reject)
  })
}

async function rosterOf(xmpp) {
  const reply = await xmpp.iqCaller.request(xml('iq', { type: 'get' }, xml('query', { xmlns: 'jabber:iq:roster' })))
  return reply.getChild('query').getChildren('item').map((item) => ({ jid: item.attrs.jid, subscription: item.attrs.subscription }))
}

// ------------------------------------------------------------------ test

const users = []
const connections = []
let hub
let exitCode = 0
try {
  const alice = await createUser('a')
  const bob = await createUser('b')
  const carol = await createUser('c') // not a friend of alice
  users.push(alice, bob, carol)
  const jid = (u) => `${u.username.toLowerCase()}@${DOMAIN}`

  // alice and bob become friends on the website
  await api('/api/friends/requests', { method: 'POST', cookie: alice.cookie, body: { username: bob.username } })
  const incoming = (await api('/api/friends/requests', { cookie: bob.cookie })).json.incoming[0]
  const accepted = await api(`/api/friends/requests/${incoming.id}/accept`, { method: 'POST', cookie: bob.cookie })
  check('web friendship created', accepted.status === 204)

  // bob listens to the website presence (SignalR) like his browser would
  const events = []
  hub = new signalR.HubConnectionBuilder()
    .withUrl(`${API}/hubs/presence`, { headers: { Cookie: bob.cookie }, transport: signalR.HttpTransportType.WebSockets, skipNegotiation: true })
    .configureLogging(signalR.LogLevel.Error)
    .build()
  hub.on('PresenceChanged', (change) => {
    events.push(`${change.username}:${change.status}`)
  })
  await hub.start()
  await sleep(500)

  // Jabber clients
  const a = await connect(alice)
  const b = await connect(bob)
  const c = await connect(carol)
  connections.push(a, b, c)
  await a.xmpp.send(xml('presence'))
  await b.xmpp.send(xml('presence'))
  await c.xmpp.send(xml('presence'))

  // roster
  const aliceRoster = await waitFor(async () => {
    const roster = await rosterOf(a.xmpp)
    return roster.find((r) => r.jid === jid(bob)) ? roster : null
  })
  check('friend is in the Jabber roster (subscription both)', aliceRoster?.find((r) => r.jid === jid(bob))?.subscription === 'both', JSON.stringify(aliceRoster))

  // presence of a Jabber-only user on the website
  const snapshot = await hub.invoke('GetSnapshot')
  check('Jabber session of a friend shows as online on the website', snapshot.some((p) => p.username === alice.username && p.status === 'online'), JSON.stringify(snapshot.map((p) => `${p.username}:${p.status}`)))

  await a.xmpp.send(xml('presence', {}, xml('show', {}, 'away')))
  check('Jabber "away" shows as AFK on the website', !!(await waitFor(() => events.includes(`${alice.username}:afk`))), events.join(','))
  await a.xmpp.send(xml('presence'))
  const backOnline = await waitFor(() => events.lastIndexOf(`${alice.username}:online`) > events.indexOf(`${alice.username}:afk`))
  check('Jabber available again shows as online', !!backOnline)

  // Jabber -> web
  await a.xmpp.send(xml('message', { type: 'chat', to: jid(bob), id: 'j2w' }, xml('body', {}, 'hello from Jabber')))
  check('bob receives it in his Jabber client', !!(await waitFor(() => b.received.find((m) => m.body === 'hello from Jabber'))))
  const chatId = await waitFor(async () => {
    const conversations = (await api('/api/personal-chats', { cookie: bob.cookie })).json
    return conversations.find((conv) => conv.username === alice.username)?.chatId
  })
  const webMessages = chatId ? (await api(`/api/chats/${chatId}/messages`, { cookie: bob.cookie })).json.messages : []
  const mirrored = webMessages.find((m) => m.text === 'hello from Jabber')
  check('the Jabber message appears in the web personal chat', !!mirrored && mirrored.authorUsername === alice.username)

  // web -> Jabber
  const send = await api(`/api/chats/${chatId}/messages`, { method: 'POST', cookie: bob.cookie, body: { text: 'hello from the website' } })
  check('web message accepted', send.status === 201)
  const delivered = await waitFor(() => a.received.find((m) => m.body === 'hello from the website'))
  check('alice receives the web message in her Jabber client', !!delivered && delivered.from.startsWith(`${bob.username.toLowerCase()}@`), delivered?.from)
  await sleep(1500)
  const afterWeb = (await api(`/api/chats/${chatId}/messages`, { cookie: bob.cookie })).json.messages
  check('no duplicate of the web message (no feedback loop)', afterWeb.filter((m) => m.text === 'hello from the website').length === 1)

  // not friends: delivered by Jabber, but not mirrored into any web chat
  await a.xmpp.send(xml('message', { type: 'chat', to: jid(carol), id: 'nf' }, xml('body', {}, 'hello stranger')))
  check('carol receives it in her Jabber client', !!(await waitFor(() => c.received.find((m) => m.body === 'hello stranger'))))
  await sleep(1500)
  const carolChats = (await api('/api/personal-chats', { cookie: carol.cookie })).json
  check('non-friends are not mirrored into the web chat', carolChats.length === 0)

  // disconnect: offline on the website
  await a.xmpp.stop()
  check('closing the Jabber session shows offline on the website', !!(await waitFor(() => events.lastIndexOf(`${alice.username}:offline`) > events.lastIndexOf(`${alice.username}:online`))), events.join(','))

  exitCode = results.every(Boolean) ? 0 : 1
  console.log(exitCode === 0 ? `\nAll ${results.length} checks passed.` : `\n${results.filter((r) => !r).length} of ${results.length} checks failed.`)
} catch (error) {
  exitCode = 1
  console.log('ERROR', error.message)
} finally {
  await hub?.stop().catch(() => {})
  await Promise.allSettled(connections.map((c) => c.xmpp.stop()))
  for (const user of users) {
    const login = await api('/api/auth/login', { method: 'POST', body: { email: user.email, password, keepSignedIn: false } })
    if (login.status === 200) await api('/api/auth/account', { method: 'DELETE', cookie: login.cookie, body: { password } })
  }
}
process.exit(exitCode)
