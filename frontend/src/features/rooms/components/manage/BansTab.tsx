import { useCallback, useEffect, useState } from 'react'
import { usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../../api/roomsApi'
import type { RoomBan } from '../../types'

const dateTime = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

/** Banned users: who banned each user and when, with Unban. */
export default function BansTab({ roomId }: { roomId: string }) {
  const { subscribe } = usePresence()
  const [bans, setBans] = useState<RoomBan[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string>()

  const load = useCallback(async () => {
    try {
      setBans(await roomsApi.bans(roomId))
      setError('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load the ban list.')
    } finally {
      setLoading(false)
    }
  }, [roomId])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void load()
    return subscribe('RoomsChanged', () => void load())
  }, [load, subscribe])

  async function unban(userId: string) {
    setBusyId(userId)
    try {
      await roomsApi.unban(roomId, userId)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not unban the user.')
    } finally {
      setBusyId(undefined)
    }
  }

  if (loading) return <p className="page-loading">Loading…</p>

  return (
    <div>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {bans.length === 0 ? (
        <p className="contacts-empty">No banned users.</p>
      ) : (
        <div className="table-scroll">
          <table className="manage-table">
            <thead>
              <tr>
                <th>Username</th>
                <th>Banned by</th>
                <th>Date/time</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {bans.map((b) => (
                <tr key={b.userId}>
                  <td>{b.username}</td>
                  <td>{b.bannedByUsername}</td>
                  <td>{dateTime.format(new Date(b.bannedAt))}</td>
                  <td className="manage-actions">
                    <button type="button" className="link-button" disabled={busyId === b.userId} onClick={() => unban(b.userId)}>
                      Unban
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
