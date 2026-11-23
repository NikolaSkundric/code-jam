import { NavLink } from 'react-router-dom'
import type { RoomSummary } from '../types'

/** Sidebar list of rooms (one visibility group) with unread counters. */
export default function RoomList({ rooms }: { rooms: RoomSummary[] }) {
  if (rooms.length === 0) return <p className="room-group-empty">None yet</p>

  return (
    <ul className="room-list">
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
  )
}
