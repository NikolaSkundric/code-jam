import { useState } from 'react'
import { Link } from 'react-router-dom'
import { PresenceDot, usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { friendsApi } from '../api/friendsApi'
import { useFriends } from '../useFriends'
import type { Friend } from '../types'

type Action = 'remove' | 'ban'

export default function FriendsList() {
  const { friends, loading, reload } = useFriends()
  const { statusOf } = usePresence()
  const [confirming, setConfirming] = useState<{ userId: string; action: Action }>()
  const [busyId, setBusyId] = useState<string>()
  const [error, setError] = useState('')

  async function perform(friend: Friend, action: Action) {
    setBusyId(friend.userId)
    setError('')
    try {
      if (action === 'remove') await friendsApi.remove(friend.userId)
      else await friendsApi.ban(friend.username)
      setConfirming(undefined)
      await reload()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <section className="contacts-card" aria-labelledby="friends-title">
      <h2 id="friends-title">
        Friends <span className="contacts-count">({friends.length})</span>
      </h2>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      {loading ? (
        <p className="page-loading">Loading…</p>
      ) : friends.length === 0 ? (
        <p className="contacts-empty">You have no friends yet. Send a friend request to get started.</p>
      ) : (
        <ul className="contact-list">
          {friends.map((friend) => {
            const isConfirming = confirming?.userId === friend.userId
            const busy = busyId === friend.userId
            return (
              <li key={friend.userId} className="contact-item">
                <div className="contact-info">
                  <p className="contact-name">
                    <PresenceDot status={statusOf(friend.userId)} showLabel /> <span>{friend.username}</span>
                  </p>
                  {isConfirming && confirming.action === 'ban' && (
                    <p className="contact-note">
                      {friend.username} will no longer be able to contact you in any way. The friendship ends and
                      your personal chat becomes read-only.
                    </p>
                  )}
                </div>
                <div className="contact-actions">
                  {isConfirming ? (
                    <>
                      <button
                        type="button"
                        className="btn-danger"
                        disabled={busy}
                        onClick={() => perform(friend, confirming.action)}
                      >
                        {confirming.action === 'ban' ? `Ban ${friend.username}` : `Remove ${friend.username}`}
                      </button>
                      <button
                        type="button"
                        className="btn-secondary"
                        disabled={busy}
                        onClick={() => setConfirming(undefined)}
                      >
                        Cancel
                      </button>
                    </>
                  ) : (
                    <>
                      <Link className="btn-primary" to={`/messages/with/${friend.userId}`}>
                        Message
                      </Link>
                      <button
                        type="button"
                        className="btn-secondary"
                        onClick={() => setConfirming({ userId: friend.userId, action: 'remove' })}
                      >
                        Remove
                      </button>
                      <button
                        type="button"
                        className="btn-secondary"
                        onClick={() => setConfirming({ userId: friend.userId, action: 'ban' })}
                      >
                        Ban
                      </button>
                    </>
                  )}
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
