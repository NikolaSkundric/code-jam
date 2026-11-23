import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import { useAuth } from '../useAuth'

export default function DeleteAccountSection() {
  const { setUser } = useAuth()
  const navigate = useNavigate()
  const [confirming, setConfirming] = useState(false)
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string>()
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  function cancel() {
    setConfirming(false)
    setPassword('')
    setError(undefined)
    setFormError('')
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')

    if (!password) {
      setError('Enter your password to confirm')
      return
    }
    setError(undefined)

    setSubmitting(true)
    try {
      await authApi.deleteAccount({ password })
      // The server cleared the cookie: this browser is signed out now.
      setUser(null)
      navigate('/login', { replace: true })
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors.password) {
        setError(err.fieldErrors.password)
      } else {
        setFormError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
      }
      setSubmitting(false)
    }
  }

  return (
    <section className="settings-card settings-danger" aria-labelledby="delete-account-title">
      <h2 id="delete-account-title">Delete account</h2>
      <p className="settings-hint">
        Your account will be removed and you will be removed from all rooms you joined. Chat rooms you own
        are deleted permanently, together with all their messages, files and images. This cannot be undone.
      </p>

      {confirming ? (
        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          {formError && (
            <p className="form-error" role="alert">
              {formError}
            </p>
          )}

          <TextField
            label="Confirm with your password"
            type="password"
            name="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={error}
            autoFocus
          />

          <div className="settings-actions">
            <button type="submit" className="btn-danger" disabled={submitting}>
              {submitting ? 'Deleting…' : 'Permanently delete my account'}
            </button>
            <button type="button" className="btn-secondary" onClick={cancel} disabled={submitting}>
              Cancel
            </button>
          </div>
        </form>
      ) : (
        <button type="button" className="btn-danger" onClick={() => setConfirming(true)}>
          Delete account…
        </button>
      )}
    </section>
  )
}
