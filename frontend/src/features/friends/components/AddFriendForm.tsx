import { useState } from 'react'
import type { FormEvent } from 'react'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { friendsApi } from '../api/friendsApi'
import { useFriends } from '../useFriends'

export default function AddFriendForm() {
  const { reload } = useFriends()
  const [username, setUsername] = useState('')
  const [message, setMessage] = useState('')
  const [error, setError] = useState<string>()
  const [formError, setFormError] = useState('')
  const [success, setSuccess] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')
    setSuccess('')

    const name = username.trim()
    if (!name) {
      setError('Enter a username')
      return
    }
    setError(undefined)

    setSubmitting(true)
    try {
      await friendsApi.sendRequest({ username: name, message: message.trim() || undefined })
      setSuccess(`Friend request sent to ${name}.`)
      setUsername('')
      setMessage('')
      await reload()
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors.username) setError(err.fieldErrors.username)
      else if (err instanceof ApiError && err.fieldErrors.message) setError(err.fieldErrors.message)
      else setFormError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="contacts-card" aria-labelledby="add-friend-title">
      <h2 id="add-friend-title">Add a friend</h2>
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        {formError && (
          <p className="form-error" role="alert">
            {formError}
          </p>
        )}
        {success && (
          <p className="form-success" role="status">
            {success}
          </p>
        )}

        <TextField
          label="Username"
          type="text"
          name="username"
          autoComplete="off"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          error={error}
        />
        <div className="field">
          <label htmlFor="friend-message">Message (optional)</label>
          <textarea
            id="friend-message"
            name="message"
            rows={2}
            maxLength={500}
            value={message}
            onChange={(e) => setMessage(e.target.value)}
          />
        </div>

        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Sending…' : 'Send friend request'}
        </button>
      </form>
    </section>
  )
}
