import { useState } from 'react'
import { PresenceDot, usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../../api/roomsApi'
import type { RoomDetails, RoomMember } from '../../types'

const ROLE_LABEL = { owner: 'Owner', admin: 'Admin', member: 'Member' } as const

interface MembersTabProps {
  room: RoomDetails
  currentUserId: string
  onChanged: () => Promise<void>
}

/** Members list with the admin actions of the wireframe: make/remove admin, ban (remove from room). */
export default function MembersTab({ room, currentUserId, onChanged }: MembersTabProps) {
  const { statusOf } = usePresence()
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string>()
  const [confirmBanId, setConfirmBanId] = useState<string>()

  const members = room.members.filter((m) => m.username.toLowerCase().includes(search.trim().toLowerCase()))

  async function run(member: RoomMember, action: () => Promise<unknown>) {
    setBusyId(member.userId)
    setError('')
    try {
      await action()
      setConfirmBanId(undefined)
      await onChanged()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <div>
      <div className="field">
        <label htmlFor="member-search">Search member</label>
        <input id="member-search" type="text" value={search} onChange={(e) => setSearch(e.target.value)} />
      </div>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      <div className="table-scroll">
        <table className="manage-table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Status</th>
              <th>Role</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {members.map((m) => {
              const busy = busyId === m.userId
              const isSelf = m.userId === currentUserId
              // Admins manage members; only the owner manages other admins. Nobody can touch the owner.
              const canBan = !isSelf && m.role !== 'owner' && (m.role === 'member' || room.myRole === 'owner')
              return (
                <tr key={m.userId}>
                  <td>{m.username}</td>
                  <td>
                    <PresenceDot status={statusOf(m.userId)} showLabel />
                  </td>
                  <td>{ROLE_LABEL[m.role]}</td>
                  <td className="manage-actions">
                    {m.role === 'owner' && '--'}
                    {m.role === 'member' && (
                      <button type="button" className="link-button" disabled={busy} onClick={() => run(m, () => roomsApi.setAdmin(room.id, m.userId, true))}>
                        Make admin
                      </button>
                    )}
                    {m.role === 'admin' && (
                      <button type="button" className="link-button" disabled={busy} onClick={() => run(m, () => roomsApi.setAdmin(room.id, m.userId, false))}>
                        Remove admin
                      </button>
                    )}
                    {canBan &&
                      (confirmBanId === m.userId ? (
                        <>
                          <button type="button" className="link-button danger" disabled={busy} onClick={() => run(m, () => roomsApi.ban(room.id, m.userId))}>
                            Confirm ban
                          </button>
                          <button type="button" className="link-button" onClick={() => setConfirmBanId(undefined)}>
                            Cancel
                          </button>
                        </>
                      ) : (
                        <button type="button" className="link-button danger" disabled={busy} onClick={() => setConfirmBanId(m.userId)}>
                          Ban
                        </button>
                      ))}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
      <p className="contacts-note-text">Removing a member bans them: they cannot rejoin until unbanned.</p>
    </div>
  )
}
