import { useState } from 'react'
import { ApiError } from '@/lib/http'
import { friendsApi } from '../api/friendsApi'
import { useFriends } from '../useFriends'
import type { FriendRequest } from '../types'

export default function FriendRequestsSection() {
  const { incoming, outgoing, reload } = useFriends()
  const [busyId, setBusyId] = useState<string>()
  const [error, setError] = useState('')

  async function run(request: FriendRequest, action: (id: string) => Promise<unknown>) {
    setBusyId(request.id)
    setError('')
    try {
      await action(request.id)
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
      await reload()
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <section className="contacts-card" aria-labelledby="requests-title">
      <h2 id="requests-title">
        Friend requests
        {incoming.length > 0 && <span className="badge">{incoming.length}</span>}
      </h2>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      <h3>Received</h3>
      {incoming.length === 0 ? (
        <p className="contacts-empty">No pending requests.</p>
      ) : (
        <ul className="contact-list">
          {incoming.map((request) => (
            <li key={request.id} className="contact-item">
              <div className="contact-info">
                <p className="contact-name">{request.username}</p>
                {request.message && <p className="contact-note">“{request.message}”</p>}
              </div>
              <div className="contact-actions">
                <button
                  type="button"
                  className="btn-primary"
                  disabled={busyId === request.id}
                  onClick={() => run(request, friendsApi.accept)}
                >
                  Accept
                </button>
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={busyId === request.id}
                  onClick={() => run(request, friendsApi.decline)}
                >
                  Decline
                </button>
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={busyId === request.id}
                  onClick={() => run(request, () => friendsApi.ban(request.username))}
                >
                  Ban
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <h3>Sent</h3>
      {outgoing.length === 0 ? (
        <p className="contacts-empty">No sent requests.</p>
      ) : (
        <ul className="contact-list">
          {outgoing.map((request) => (
            <li key={request.id} className="contact-item">
              <div className="contact-info">
                <p className="contact-name">{request.username}</p>
                <p className="contact-note">Waiting for a response</p>
              </div>
              <div className="contact-actions">
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={busyId === request.id}
                  onClick={() => run(request, friendsApi.cancel)}
                >
                  Cancel
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
