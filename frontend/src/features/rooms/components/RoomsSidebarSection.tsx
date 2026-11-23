import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../api/roomsApi'
import { useRooms } from '../useRooms'
import type { RoomSummary } from '../types'
import CreateRoomModal from './CreateRoomModal'

function RoomGroup({ title, rooms }: { title: string; rooms: RoomSummary[] }) {
  const unread = rooms.reduce((sum, r) => sum + r.unreadCount, 0)
  return (
    <details className="room-group" open>
      <summary>
        {title}
        {unread > 0 && <span className="nav-badge">{unread}</span>}
      </summary>
      {rooms.length === 0 ? (
        <p className="room-group-empty">None yet</p>
      ) : (
        <ul>
          {rooms.map((room) => (
            <li key={room.id}>
              <NavLink to={`/rooms/${room.id}`} className="conversation">
                <span className="conversation-body">
                  <span className="conversation-name"># {room.name}</span>
                </span>
                {room.unreadCount > 0 && (
                  <span className="nav-badge" aria-label={`${room.unreadCount} unread messages`}>
                    {room.unreadCount}
                  </span>
                )}
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </details>
  )
}

/** Sidebar: public and private rooms (accordion), invitations, create room and the public catalog. */
export default function RoomsSidebarSection() {
  const { rooms, invitations, reload } = useRooms()
  const navigate = useNavigate()
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState('')

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
    <section className="sidebar-section" aria-label="Rooms">
      <h2>Rooms</h2>

      {invitations.length > 0 && (
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
      )}

      <RoomGroup title="Public rooms" rooms={rooms.filter((r) => r.visibility === 'public')} />
      <RoomGroup title="Private rooms" rooms={rooms.filter((r) => r.visibility === 'private')} />

      <div className="sidebar-actions">
        <button type="button" className="btn-primary" onClick={() => setCreating(true)}>
          Create room
        </button>
        <NavLink to="/rooms" end className="btn-secondary sidebar-link">
          Browse public rooms
        </NavLink>
      </div>

      {creating && (
        <CreateRoomModal
          onClose={() => setCreating(false)}
          onCreated={async (roomId) => {
            setCreating(false)
            await reload()
            navigate(`/rooms/${roomId}`)
          }}
        />
      )}
    </section>
  )
}
