import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import AuthLayout from '../components/AuthLayout'
import TextField from '@/components/ui/TextField'
import { useAuth } from '../useAuth'
import { validateEmail } from '../utils/validation'

export default function LoginPage() {
  const navigate = useNavigate()
  const { setUser } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [keepSignedIn, setKeepSignedIn] = useState(true)
  const [errors, setErrors] = useState<{ email?: string; password?: string }>({})
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')

    const nextErrors = {
      email: validateEmail(email),
      password: password ? undefined : 'Password is required',
    }
    setErrors(nextErrors)
    if (nextErrors.email || nextErrors.password) return

    setSubmitting(true)
    try {
      const user = await authApi.login({ email: email.trim(), password, keepSignedIn })
      setUser(user)
      navigate('/', { replace: true })
    } catch (error) {
      setFormError(
        error instanceof ApiError && error.status === 401
          ? 'Invalid email or password.'
          : error instanceof ApiError
            ? error.message
            : 'Something went wrong. Please try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Sign in">
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
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
          error={errors.email}
        />
        <TextField
          label="Password"
          type="password"
          name="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={errors.password}
        />

        <label className="checkbox">
          <input
            type="checkbox"
            checked={keepSignedIn}
            onChange={(e) => setKeepSignedIn(e.target.checked)}
          />
          Keep me signed in
        </label>

        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>

        <p className="auth-links">
          <Link to="/forgot-password">Forgot password?</Link>
          <span>
            No account? <Link to="/register">Register</Link>
          </span>
        </p>
      </form>
    </AuthLayout>
  )
}
