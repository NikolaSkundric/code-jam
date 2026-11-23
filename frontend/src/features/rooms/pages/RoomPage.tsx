import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { MessageThread } from '@/features/chat'
import { usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../api/roomsApi'
import InviteModal from '../components/InviteModal'
import ManageRoomModal from '../components/ManageRoomModal'
import RoomMembersPanel from '../components/RoomMembersPanel'
import { useRooms } from '../useRooms'
import { isAdminRole } from '../types'
import type { RoomDetails } from '../types'
import './rooms.css'
import '@/features/chat/pages/chat.css'
import '@/features/chat/pages/attachments.css'

function RoomView({ roomId }: { roomId: string }) {
  const navigate = useNavigate()
  const { subscribe } = usePresence()
  const { reload: reloadRooms } = useRooms()
  const [room, setRoom] = useState<RoomDetails>()
  const [error, setError] = useState('')
  const [inviting, setInviting] = useState(false)
  const [managing, setManaging] = useState(false)

  const leaveToHome = useCallback(async () => {
    await reloadRooms()
    navigate('/messages', { replace: true })
  }, [navigate, reloadRooms])

  const load = useCallback(async () => {
    try {
      setRoom(await roomsApi.get(roomId))
      setError('')
    } catch (err) {
      if (err instanceof ApiError && (err.status === 404 || err.status === 403)) {
        // Removed, banned or the room was deleted: access is gone.
        await leaveToHome()
      } else {
        setError(err instanceof ApiError ? err.message : 'Could not load the room.')
      }
    }
  }, [roomId, leaveToHome])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void load()
    return subscribe('RoomsChanged', (payload) => {
      if (payload === roomId || payload == null) void load()
    })
  }, [load, subscribe, roomId])

  async function leave() {
    try {
      await roomsApi.leave(roomId)
      await leaveToHome()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not leave the room.')
    }
  }

  if (!room) {
    return (
      <div className="chat-main">
        {error ? (
          <p className="form-error" role="alert" style={{ margin: 24 }}>
            {error}
          </p>
        ) : (
          <p className="page-loading">Loading…</p>
        )}
      </div>
    )
  }

  return (
    <div className="room-view">
      <div className="chat-main">
        <MessageThread
          key={room.id}
          chatId={room.id}
          title={`# ${room.name}`}
          subtitle={room.description || undefined}
          canWrite
          canModerate={isAdminRole(room.myRole)}
          onRead={reloadRooms}
        />
      </div>

      <RoomMembersPanel room={room} onInvite={() => setInviting(true)} onManage={() => setManaging(true)} onLeave={leave} />

      {inviting && <InviteModal roomId={room.id} roomName={room.name} onClose={() => setInviting(false)} />}
      {managing && (
        <ManageRoomModal
          room={room}
          onChanged={load}
          onDeleted={() => {
            setManaging(false)
            void leaveToHome()
          }}
          onClose={() => setManaging(false)}
        />
      )}
    </div>
  )
}

/** A room: message thread (center) and members panel (right). */
export default function RoomPage() {
  const { roomId } = useParams()
  return roomId ? <RoomView key={roomId} roomId={roomId} /> : null
}
