import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useRooms } from '../useRooms'
import CreateRoomModal from './CreateRoomModal'

/** Sidebar buttons: create a room and browse the public catalog. */
export default function RoomActions() {
  const { reload } = useRooms()
  const navigate = useNavigate()
  const [creating, setCreating] = useState(false)

  return (
    <div className="sidebar-actions">
      <button type="button" className="btn-primary" onClick={() => setCreating(true)}>
        Create room
      </button>
      <NavLink to="/rooms" end className="btn-secondary sidebar-link">
        Browse public rooms
      </NavLink>

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
    </div>
  )
}
