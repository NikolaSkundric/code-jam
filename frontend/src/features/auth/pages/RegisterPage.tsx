import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import AuthLayout from '../components/AuthLayout'
import TextField from '@/components/ui/TextField'
import {
  validateConfirmPassword,
  validateEmail,
  validatePassword,
  validateUsername,
} from '../utils/validation'
import { useAuth } from '../useAuth'

type FieldErrors = Partial<Record<'email' | 'username' | 'password' | 'confirmPassword', string>>

export default function RegisterPage() {
  const navigate = useNavigate()
  const { setUser } = useAuth()
  const [email, setEmail] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')

    const nextErrors: FieldErrors = {
      email: validateEmail(email),
      username: validateUsername(username),
      password: validatePassword(password),
      confirmPassword: validateConfirmPassword(password, confirmPassword),
    }
    setErrors(nextErrors)
    if (Object.values(nextErrors).some(Boolean)) return

    setSubmitting(true)
    try {
      const user = await authApi.register({ email: email.trim(), username: username.trim(), password })
      setUser(user)
      navigate('/', { replace: true })
    } catch (error) {
      if (error instanceof ApiError) {
        // Server-side field errors (e.g. email/username already taken).
        const serverErrors: FieldErrors = {}
        for (const field of ['email', 'username', 'password'] as const) {
          if (error.fieldErrors[field]) serverErrors[field] = error.fieldErrors[field]
        }
        setErrors(serverErrors)
        if (Object.keys(serverErrors).length === 0) setFormError(error.message)
      } else {
        setFormError('Something went wrong. Please try again.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Register">
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
          label="Username"
          type="text"
          name="username"
          autoComplete="username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          error={errors.username}
        />
        <TextField
          label="Password"
          type="password"
          name="password"
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={errors.password}
        />
        <TextField
          label="Confirm password"
          type="password"
          name="confirmPassword"
          autoComplete="new-password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          error={errors.confirmPassword}
        />

        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Creating account…' : 'Create account'}
        </button>

        <p className="auth-links">
          <span>
            Already registered? <Link to="/login">Sign in</Link>
          </span>
        </p>
      </form>
    </AuthLayout>
  )
}
