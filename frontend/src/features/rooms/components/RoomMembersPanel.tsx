import { useState } from 'react'
import { useAuth } from '@/features/auth'
import { useFriends } from '@/features/friends'
import { friendsApi } from '@/features/friends'
import { PresenceDot, usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { isAdminRole } from '../types'
import type { RoomDetails, RoomMember } from '../types'

interface RoomMembersPanelProps {
  room: RoomDetails
  onInvite: () => void
  onManage: () => void
  onLeave: () => void
}

/** Right side of a room (wireframe): room info, admins, members with presence, invite / manage. */
export default function RoomMembersPanel({ room, onInvite, onManage, onLeave }: RoomMembersPanelProps) {
  const { user } = useAuth()
  const { statusOf } = usePresence()
  const { friends, incoming, outgoing, reload: reloadFriends } = useFriends()
  const [notes, setNotes] = useState<Record<string, string>>({})

  const admins = room.members.filter((m) => isAdminRole(m.role))
  const knownIds = new Set([...friends.map((f) => f.userId), ...incoming.map((r) => r.userId), ...outgoing.map((r) => r.userId)])

  // TASK.md 2.3.2: friend requests can be sent from the user list of a room.
  async function addFriend(member: RoomMember) {
    try {
      await friendsApi.sendRequest({ username: member.username })
      setNotes((n) => ({ ...n, [member.userId]: 'Request sent' }))
      await reloadFriends()
    } catch (err) {
      const message = err instanceof ApiError ? (err.fieldErrors.username ?? err.message) : 'Could not send the request'
      setNotes((n) => ({ ...n, [member.userId]: message }))
    }
  }

  return (
    <aside className="room-panel" aria-label="Room members">
      <section>
        <h3>Room info</h3>
        <p className="room-panel-line">{room.visibility === 'public' ? 'Public room' : 'Private room'}</p>
        <p className="room-panel-line">Owner: {room.ownerUsername}</p>
        {room.description && <p className="room-panel-description">{room.description}</p>}
      </section>

      <section>
        <h3>Admins</h3>
        <ul className="room-panel-list">
          {admins.map((m) => (
            <li key={m.userId}>- {m.username}</li>
          ))}
        </ul>
      </section>

      <section>
        <h3>Members ({room.members.length})</h3>
        <ul className="room-panel-list">
          {room.members.map((m) => (
            <li key={m.userId} className="room-member">
              <PresenceDot status={statusOf(m.userId)} />
              <span className="room-member-name">{m.username}</span>
              {m.userId !== user?.id && !knownIds.has(m.userId) && !notes[m.userId] && (
                <button type="button" className="link-button" onClick={() => addFriend(m)}>
                  Add friend
                </button>
              )}
              {notes[m.userId] && <span className="muted room-member-note">{notes[m.userId]}</span>}
            </li>
          ))}
        </ul>
      </section>

      <div className="room-panel-actions">
        <button type="button" className="btn-secondary" onClick={onInvite}>
          Invite user
        </button>
        {isAdminRole(room.myRole) && (
          <button type="button" className="btn-primary" onClick={onManage}>
            Manage room
          </button>
        )}
        {room.myRole !== 'owner' && (
          <button type="button" className="btn-secondary" onClick={onLeave}>
            Leave room
          </button>
        )}
      </div>
    </aside>
  )
}
