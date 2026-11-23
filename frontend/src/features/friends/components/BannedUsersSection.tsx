import { useState } from 'react'
import type { FormEvent } from 'react'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { friendsApi } from '../api/friendsApi'
import { useFriends } from '../useFriends'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })

export default function BannedUsersSection() {
  const { banned, reload } = useFriends()
  const [username, setUsername] = useState('')
  const [fieldError, setFieldError] = useState<string>()
  const [formError, setFormError] = useState('')
  const [busy, setBusy] = useState(false)
  const [busyId, setBusyId] = useState<string>()

  async function handleBan(event: FormEvent) {
    event.preventDefault()
    setFormError('')
    const name = username.trim()
    if (!name) {
      setFieldError('Enter a username')
      return
    }
    setFieldError(undefined)

    setBusy(true)
    try {
      await friendsApi.ban(name)
      setUsername('')
      await reload()
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors.username) setFieldError(err.fieldErrors.username)
      else setFormError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusy(false)
    }
  }

  async function handleUnban(userId: string) {
    setBusyId(userId)
    setFormError('')
    try {
      await friendsApi.unban(userId)
      await reload()
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <section className="contacts-card" aria-labelledby="banned-title">
      <h2 id="banned-title">
        Banned users <span className="contacts-count">({banned.length})</span>
      </h2>
      <p className="contacts-note-text">
        A banned user cannot contact you in any way. Your friendship ends and the personal chat history stays
        visible but read-only.
      </p>

      {formError && (
        <p className="form-error" role="alert">
          {formError}
        </p>
      )}

      {banned.length === 0 ? (
        <p className="contacts-empty">You have not banned anyone.</p>
      ) : (
        <ul className="contact-list">
          {banned.map((user) => (
            <li key={user.userId} className="contact-item">
              <div className="contact-info">
                <p className="contact-name">{user.username}</p>
                <p className="contact-note">Banned {dateFormat.format(new Date(user.bannedAt))}</p>
              </div>
              <div className="contact-actions">
                <button
                  type="button"
                  className="btn-secondary"
                  disabled={busyId === user.userId}
                  onClick={() => handleUnban(user.userId)}
                >
                  Unban
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <form className="auth-form ban-form" onSubmit={handleBan} noValidate>
        <TextField
          label="Ban a user by username"
          type="text"
          name="banUsername"
          autoComplete="off"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          error={fieldError}
        />
        <button type="submit" className="btn-danger" disabled={busy}>
          {busy ? 'Banning…' : 'Ban user'}
        </button>
      </form>
    </section>
  )
}
