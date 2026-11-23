import { useState } from 'react'
import type { FormEvent } from 'react'
import Modal from '@/components/ui/Modal'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../api/roomsApi'

interface InviteModalProps {
  roomId: string
  roomName: string
  onClose: () => void
}

export default function InviteModal({ roomId, roomName, onClose }: InviteModalProps) {
  const [username, setUsername] = useState('')
  const [error, setError] = useState<string>()
  const [success, setSuccess] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSuccess('')
    if (!username.trim()) {
      setError('Enter a username')
      return
    }
    setError(undefined)

    setSubmitting(true)
    try {
      await roomsApi.invite(roomId, username.trim())
      setSuccess(`Invitation sent to ${username.trim()}.`)
      setUsername('')
    } catch (err) {
      setError(
        err instanceof ApiError ? (err.fieldErrors.username ?? err.message) : 'Something went wrong. Please try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal title={`Invite to ${roomName}`} onClose={onClose}>
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        {success && (
          <p className="form-success" role="status">
            {success}
          </p>
        )}
        <TextField
          label="Invite by username"
          type="text"
          name="username"
          autoComplete="off"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          error={error}
          autoFocus
        />
        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Sending…' : 'Send invite'}
        </button>
      </form>
    </Modal>
  )
}
