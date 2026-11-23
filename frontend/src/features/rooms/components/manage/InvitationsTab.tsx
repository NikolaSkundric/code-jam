import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import TextField from '@/components/ui/TextField'
import { usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../../api/roomsApi'
import type { RoomInvitation } from '../../types'

/** Pending invitations (cancel) and an invite-by-username form. */
export default function InvitationsTab({ roomId }: { roomId: string }) {
  const { subscribe } = usePresence()
  const [invitations, setInvitations] = useState<RoomInvitation[]>([])
  const [username, setUsername] = useState('')
  const [fieldError, setFieldError] = useState<string>()
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      setInvitations(await roomsApi.invitations(roomId))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load invitations.')
    }
  }, [roomId])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void load()
    return subscribe('RoomsChanged', () => void load())
  }, [load, subscribe])

  async function invite(event: FormEvent) {
    event.preventDefault()
    setSuccess('')
    if (!username.trim()) {
      setFieldError('Enter a username')
      return
    }
    setFieldError(undefined)
    setBusy(true)
    try {
      await roomsApi.invite(roomId, username.trim())
      setSuccess(`Invitation sent to ${username.trim()}.`)
      setUsername('')
      await load()
    } catch (err) {
      setFieldError(err instanceof ApiError ? (err.fieldErrors.username ?? err.message) : 'Something went wrong.')
    } finally {
      setBusy(false)
    }
  }

  async function cancel(id: string) {
    try {
      await roomsApi.cancelInvitation(roomId, id)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not cancel the invitation.')
    }
  }

  return (
    <div>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {invitations.length === 0 ? (
        <p className="contacts-empty">No pending invitations.</p>
      ) : (
        <ul className="manage-list">
          {invitations.map((i) => (
            <li key={i.id}>
              <span>
                {i.username} <span className="muted">invited by {i.invitedByUsername}</span>
              </span>
              <button type="button" className="link-button" onClick={() => cancel(i.id)}>
                Cancel
              </button>
            </li>
          ))}
        </ul>
      )}

      <form className="auth-form ban-form" onSubmit={invite} noValidate>
        {success && (
          <p className="form-success" role="status">
            {success}
          </p>
        )}
        <TextField
          label="Invite by username"
          type="text"
          name="inviteUsername"
          autoComplete="off"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          error={fieldError}
        />
        <button type="submit" className="btn-primary" disabled={busy}>
          Send invite
        </button>
      </form>
    </div>
  )
}
