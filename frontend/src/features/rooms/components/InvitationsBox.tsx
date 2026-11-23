import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../api/roomsApi'
import { useRooms } from '../useRooms'

/** Pending room invitations with Accept / Decline; renders nothing when there are none. */
export default function InvitationsBox() {
  const { invitations, reload } = useRooms()
  const navigate = useNavigate()
  const [error, setError] = useState('')

  if (invitations.length === 0) return null

  async function respond(id: string, accept: boolean, roomId: string) {
    setError('')
    try {
      if (accept) {
        await roomsApi.acceptInvitation(id)
        await reload()
        navigate(`/rooms/${roomId}`)
      } else {
        await roomsApi.declineInvitation(id)
        await reload()
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
      await reload()
    }
  }

  return (
    <div className="invitation-box">
      <p className="invitation-title">Invitations</p>
      {invitations.map((invitation) => (
        <div key={invitation.id} className="invitation">
          <p>
            <strong>{invitation.roomName}</strong>
            <span className="muted"> from {invitation.invitedByUsername}</span>
          </p>
          <div className="invitation-actions">
            <button type="button" className="btn-primary" onClick={() => respond(invitation.id, true, invitation.roomId)}>
              Accept
            </button>
            <button type="button" className="btn-secondary" onClick={() => respond(invitation.id, false, invitation.roomId)}>
              Decline
            </button>
          </div>
        </div>
      ))}
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
