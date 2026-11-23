import { useCallback, useEffect, useState } from 'react'
import { ApiError } from '@/lib/http'
import { jabberApi } from '../api/jabberApi'
import type { XmppDashboard, XmppServer } from '../types'
import './jabber.css'

const REFRESH_MS = 5000
const number = new Intl.NumberFormat()

function formatDuration(totalSeconds: number) {
  const days = Math.floor(totalSeconds / 86400)
  const hours = Math.floor((totalSeconds % 86400) / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = Math.floor(totalSeconds % 60)
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h ${minutes}m`
  if (minutes > 0) return `${minutes}m ${seconds}s`
  return `${seconds}s`
}

function Tile({ label, value }: { label: string; value: string }) {
  return (
    <div className="tile">
      <p className="tile-value">{value}</p>
      <p className="tile-label">{label}</p>
    </div>
  )
}

function ServerPanel({ server, role }: { server: XmppServer; role: string }) {
  const trafficTotals = server.traffic.reduce(
    (sum, t) => ({ out: sum.out + t.messagesOut, in: sum.in + t.messagesIn }),
    { out: 0, in: 0 },
  )

  return (
    <section className="jabber-panel" aria-label={`Server ${server.name}`}>
      <header className="jabber-panel-header">
        <h2>
          Server {server.name} <span className="muted">{server.domain}</span>
        </h2>
        <span className={`server-state ${server.available ? 'up' : 'down'}`}>{server.available ? 'Online' : 'Unreachable'}</span>
      </header>
      <p className="jabber-role">{role}</p>

      {!server.available ? (
        <p className="form-error">{server.error ?? 'The server is not reachable.'}</p>
      ) : (
        <>
          <div className="tiles">
            <Tile label="Connected users" value={number.format(server.connectedUsers)} />
            <Tile label="Federation links in" value={number.format(server.incomingServerConnections)} />
            <Tile label="Federation links out" value={number.format(server.outgoingServerConnections)} />
            <Tile label="Federated messages in" value={number.format(trafficTotals.in)} />
            <Tile label="Federated messages out" value={number.format(trafficTotals.out)} />
            <Tile label={`Uptime${server.version ? ` (ejabberd ${server.version})` : ''}`} value={formatDuration(server.uptimeSeconds)} />
          </div>

          <h3>Connections</h3>
          {server.sessions.length === 0 ? (
            <p className="contacts-empty">No Jabber clients are connected.</p>
          ) : (
            <div className="table-scroll">
              <table className="manage-table">
                <thead>
                  <tr>
                    <th>Jabber ID</th>
                    <th>Connection</th>
                    <th>Address</th>
                    <th>Connected for</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {server.sessions.map((s) => (
                    <tr key={`${s.jid}-${s.ip}-${s.port}`}>
                      <td>{s.jid}</td>
                      <td>{s.connection}</td>
                      <td>
                        {s.ip.replace('::ffff:', '')}:{s.port}
                      </td>
                      <td>{formatDuration(s.uptimeSeconds)}</td>
                      <td>{s.status || 'available'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <h3>Federation traffic</h3>
          {server.traffic.length === 0 ? (
            <p className="contacts-empty">No stanzas have been exchanged with other servers yet.</p>
          ) : (
            <div className="table-scroll">
              <table className="manage-table">
                <thead>
                  <tr>
                    <th>Remote server</th>
                    <th>Messages out</th>
                    <th>Messages in</th>
                    <th>Presence out</th>
                    <th>Presence in</th>
                    <th>IQ out</th>
                    <th>IQ in</th>
                  </tr>
                </thead>
                <tbody>
                  {server.traffic.map((t) => (
                    <tr key={t.peer}>
                      <td>{t.peer}</td>
                      <td>{number.format(t.messagesOut)}</td>
                      <td>{number.format(t.messagesIn)}</td>
                      <td>{number.format(t.presenceOut)}</td>
                      <td>{number.format(t.presenceIn)}</td>
                      <td>{number.format(t.iqOut)}</td>
                      <td>{number.format(t.iqIn)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </section>
  )
}

/** Admin: Jabber connection dashboard and federation traffic statistics (TASK.md section 6). */
export default function JabberDashboardPage() {
  const [dashboard, setDashboard] = useState<XmppDashboard>()
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    try {
      setDashboard(await jabberApi.dashboard())
      setError('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load the dashboard.')
    }
  }, [])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void load()
    const timer = window.setInterval(() => void load(), REFRESH_MS)
    return () => window.clearInterval(timer)
  }, [load])

  return (
    <section className="jabber-page">
      <h1>Jabber administration</h1>
      <p className="settings-hint">
        Live view of the Jabber (XMPP) servers, refreshed every {REFRESH_MS / 1000} seconds
        {dashboard && <> · updated {new Date(dashboard.generatedAt).toLocaleTimeString()}</>}.
      </p>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {!dashboard && !error && <p className="page-loading">Loading…</p>}

      {dashboard && (
        <>
          <ServerPanel server={dashboard.local} role="This chat server: Jabber accounts are the website accounts." />
          {dashboard.peer && <ServerPanel server={dashboard.peer} role="Federated peer server: separate accounts, reached through server-to-server links." />}
        </>
      )}
    </section>
  )
}
