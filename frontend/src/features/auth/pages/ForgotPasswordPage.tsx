import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import AuthLayout from '../components/AuthLayout'
import TextField from '@/components/ui/TextField'
import { validateEmail } from '../utils/validation'

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string>()
  const [formError, setFormError] = useState('')
  const [sent, setSent] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')

    const emailError = validateEmail(email)
    setError(emailError)
    if (emailError) return

    setSubmitting(true)
    try {
      await authApi.forgotPassword({ email: email.trim() })
      setSent(true)
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Forgot password">
      {sent ? (
        <p className="form-success" role="status">
          If an account exists for that email, a reset link has been sent.
        </p>
      ) : (
        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <p className="auth-hint">Enter your email to reset password</p>

          {formError && (
            <p className="form-error" role="alert">
              {formError}
            </p>
          )}

          <TextField
            label="Email"
            type="email"
            name="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            error={error}
          />

          <button type="submit" className="btn-primary" disabled={submitting}>
            {submitting ? 'Sending…' : 'Send reset link'}
          </button>
        </form>
      )}

      <p className="auth-links">
        <Link to="/login">Back to sign in</Link>
      </p>
    </AuthLayout>
  )
}
