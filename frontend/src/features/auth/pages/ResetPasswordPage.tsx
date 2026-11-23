import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import AuthLayout from '../components/AuthLayout'
import { validateConfirmPassword, validatePassword } from '../utils/validation'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<{ password?: string; confirmPassword?: string }>({})
  const [formError, setFormError] = useState('')
  const [done, setDone] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')

    const nextErrors = {
      password: validatePassword(password),
      confirmPassword: validateConfirmPassword(password, confirmPassword),
    }
    setErrors(nextErrors)
    if (nextErrors.password || nextErrors.confirmPassword) return

    setSubmitting(true)
    try {
      await authApi.resetPassword({ token, newPassword: password })
      setDone(true)
    } catch (error) {
      if (error instanceof ApiError) {
        // "token" errors (invalid/expired link) are shown as the form error.
        const message = error.fieldErrors.token ?? error.fieldErrors.newPassword ?? error.message
        setFormError(message)
      } else {
        setFormError('Something went wrong. Please try again.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (!token) {
    return (
      <AuthLayout title="Reset password">
        <p className="form-error" role="alert">
          This reset link is incomplete. Request a new one.
        </p>
        <p className="auth-links">
          <Link to="/forgot-password">Request a new link</Link>
        </p>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Reset password">
      {done ? (
        <>
          <p className="form-success" role="status">
            Your password has been changed. You can sign in with the new password.
          </p>
          <p className="auth-links">
            <Link to="/login">Go to sign in</Link>
          </p>
        </>
      ) : (
        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <p className="auth-hint">Choose a new password for your account.</p>

          {formError && (
            <p className="form-error" role="alert">
              {formError}
            </p>
          )}

          <TextField
            label="New password"
            type="password"
            name="newPassword"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            error={errors.password}
          />
          <TextField
            label="Confirm new password"
            type="password"
            name="confirmPassword"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            error={errors.confirmPassword}
          />

          <button type="submit" className="btn-primary" disabled={submitting}>
            {submitting ? 'Saving…' : 'Change password'}
          </button>

          <p className="auth-links">
            <Link to="/login">Back to sign in</Link>
          </p>
        </form>
      )}
    </AuthLayout>
  )
}
