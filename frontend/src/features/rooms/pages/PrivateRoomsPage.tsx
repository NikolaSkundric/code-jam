import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import CreateRoomModal from '../components/CreateRoomModal'
import InvitationsBox from '../components/InvitationsBox'
import { useRooms } from '../useRooms'
import './rooms.css'

/** Top menu "Private Rooms": my private rooms and the invitations I received. */
export default function PrivateRoomsPage() {
  const { rooms, reload } = useRooms()
  const navigate = useNavigate()
  const [creating, setCreating] = useState(false)
  const privateRooms = rooms.filter((r) => r.visibility === 'private')

  return (
    <div className="chat-main catalog-page">
      <h1>Private rooms</h1>
      <p className="settings-hint">Private rooms are not listed in the catalog. You can only join them by invitation.</p>

      <InvitationsBox />

      {privateRooms.length === 0 ? (
        <p className="contacts-empty">You are not a member of any private room yet.</p>
      ) : (
        <ul className="catalog-list">
          {privateRooms.map((room) => (
            <li key={room.id} className="catalog-item">
              <div className="catalog-info">
                <p className="catalog-name"># {room.name}</p>
                {room.description && <p className="catalog-description">{room.description}</p>}
                <p className="catalog-meta">
                  {room.memberCount} {room.memberCount === 1 ? 'member' : 'members'} · you are {room.myRole}
                  {room.unreadCount > 0 && ` · ${room.unreadCount} unread`}
                </p>
              </div>
              <button type="button" className="btn-secondary" onClick={() => navigate(`/rooms/${room.id}`)}>
                Open
              </button>
            </li>
          ))}
        </ul>
      )}

      <div className="private-actions">
        <button type="button" className="btn-primary" onClick={() => setCreating(true)}>
          Create private room
        </button>
      </div>

      {creating && (
        <CreateRoomModal
          defaultVisibility="private"
          onClose={() => setCreating(false)}
          onCreated={async (roomId) => {
            setCreating(false)
            await reload()
            navigate(`/rooms/${roomId}`)
          }}
        />
      )}
    </div>
  )
}
