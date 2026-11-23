import { useState } from 'react'
import type { FormEvent } from 'react'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { authApi } from '../api/authApi'
import { validateConfirmPassword, validatePassword } from '../utils/validation'

type FieldErrors = Partial<Record<'currentPassword' | 'newPassword' | 'confirmPassword', string>>

export default function ChangePasswordForm() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState('')
  const [success, setSuccess] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')
    setSuccess(false)

    const nextErrors: FieldErrors = {
      currentPassword: currentPassword ? undefined : 'Current password is required',
      newPassword: validatePassword(newPassword),
      confirmPassword: validateConfirmPassword(newPassword, confirmPassword),
    }
    setErrors(nextErrors)
    if (Object.values(nextErrors).some(Boolean)) return

    setSubmitting(true)
    try {
      await authApi.changePassword({ currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setSuccess(true)
    } catch (error) {
      if (error instanceof ApiError) {
        const serverErrors: FieldErrors = {}
        if (error.fieldErrors.currentPassword) serverErrors.currentPassword = error.fieldErrors.currentPassword
        if (error.fieldErrors.newPassword) serverErrors.newPassword = error.fieldErrors.newPassword
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
    <section className="settings-card" aria-labelledby="change-password-title">
      <h2 id="change-password-title">Change password</h2>
      <p className="settings-hint">Other browsers where you are signed in will be signed out.</p>

      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        {formError && (
          <p className="form-error" role="alert">
            {formError}
          </p>
        )}
        {success && (
          <p className="form-success" role="status">
            Password changed. Your other sessions were signed out.
          </p>
        )}

        <TextField
          label="Current password"
          type="password"
          name="currentPassword"
          autoComplete="current-password"
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          error={errors.currentPassword}
        />
        <TextField
          label="New password"
          type="password"
          name="newPassword"
          autoComplete="new-password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          error={errors.newPassword}
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
      </form>
    </section>
  )
}
