import { useState } from 'react'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../../api/roomsApi'
import { isAdminRole } from '../../types'
import type { RoomDetails } from '../../types'

interface AdminsTabProps {
  room: RoomDetails
  onChanged: () => Promise<void>
}

export default function AdminsTab({ room, onChanged }: AdminsTabProps) {
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string>()
  const admins = room.members.filter((m) => isAdminRole(m.role))

  async function removeAdmin(userId: string) {
    setBusyId(userId)
    setError('')
    try {
      await roomsApi.setAdmin(room.id, userId, false)
      await onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <div>
      <p>Current admins: {admins.map((a) => a.username).join(', ')}</p>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      <ul className="manage-list">
        {admins.map((a) => (
          <li key={a.userId}>
            <span>
              {a.username}
              {a.role === 'owner' && <span className="muted"> = owner (cannot lose admin rights)</span>}
            </span>
            {a.role === 'admin' && (
              <button type="button" className="link-button" disabled={busyId === a.userId} onClick={() => removeAdmin(a.userId)}>
                Remove admin
              </button>
            )}
          </li>
        ))}
      </ul>
    </div>
  )
}
