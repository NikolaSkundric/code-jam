import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import { useAuth } from '../useAuth'
import type { SessionInfo } from '../types'
import { describeUserAgent } from '../utils/userAgent'
import './sessions.css'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

export default function SessionsPage() {
  const { setUser } = useAuth()
  const navigate = useNavigate()
  const [sessions, setSessions] = useState<SessionInfo[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string>()

  const load = useCallback(async () => {
    try {
      setSessions(await authApi.getSessions())
      setError('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load sessions.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void load()
  }, [load])

  async function handleLogOut(session: SessionInfo) {
    setBusyId(session.id)
    setError('')
    try {
      await authApi.revokeSession(session.id)
      if (session.isCurrent) {
        // The cookie was cleared by the server: this browser is signed out now.
        setUser(null)
        navigate('/login', { replace: true })
        return
      }
      setSessions((current) => current.filter((s) => s.id !== session.id))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not log out this session.')
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <section className="sessions-page">
      <h1>Active sessions</h1>
      <p className="sessions-hint">
        Browsers and devices where you are signed in. Logging out a session signs out only that browser.
      </p>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      {loading ? (
        <p className="page-loading">Loading…</p>
      ) : (
        <ul className="session-list">
          {sessions.map((session) => (
            <li key={session.id} className="session-item">
              <div className="session-info">
                <p className="session-title">
                  {describeUserAgent(session.userAgent)}
                  {session.isCurrent && <span className="badge">This browser</span>}
                </p>
                <p className="session-meta">
                  IP {session.ipAddress || 'unknown'} · Signed in {dateFormat.format(new Date(session.createdAt))} ·
                  Last active {dateFormat.format(new Date(session.lastSeenAt))}
                </p>
              </div>
              <button
                type="button"
                className="btn-secondary"
                onClick={() => handleLogOut(session)}
                disabled={busyId === session.id}
              >
                {session.isCurrent ? 'Sign out' : 'Log out'}
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
