import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../api/roomsApi'
import { useRooms } from '../useRooms'
import type { RoomCatalogItem } from '../types'
import './rooms.css'

/** Public room catalog with simple search (TASK.md 2.4.3). */
export default function RoomCatalogPage() {
  const { reload } = useRooms()
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [items, setItems] = useState<RoomCatalogItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<string>()

  useEffect(() => {
    let cancelled = false
    const timer = window.setTimeout(() => {
      roomsApi
        .catalog(search.trim() || undefined)
        .then((result) => {
          if (!cancelled) setItems(result)
        })
        .catch(() => {
          if (!cancelled) setError('Could not load the catalog.')
        })
        .finally(() => {
          if (!cancelled) setLoading(false)
        })
    }, 250)
    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [search])

  async function join(item: RoomCatalogItem) {
    setBusyId(item.id)
    setError('')
    try {
      await roomsApi.join(item.id)
      await reload()
      navigate(`/rooms/${item.id}`)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not join the room.')
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <div className="chat-main catalog-page">
      <h1>Public rooms</h1>
      <div className="field">
        <label htmlFor="room-search">Search</label>
        <input
          id="room-search"
          type="text"
          placeholder="Search by name or description"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      {loading ? (
        <p className="page-loading">Loading…</p>
      ) : items.length === 0 ? (
        <p className="contacts-empty">No public rooms found.</p>
      ) : (
        <ul className="catalog-list">
          {items.map((item) => (
            <li key={item.id} className="catalog-item">
              <div className="catalog-info">
                <p className="catalog-name"># {item.name}</p>
                {item.description && <p className="catalog-description">{item.description}</p>}
                <p className="catalog-meta">
                  {item.memberCount} {item.memberCount === 1 ? 'member' : 'members'}
                </p>
              </div>
              {item.isMember ? (
                <button type="button" className="btn-secondary" onClick={() => navigate(`/rooms/${item.id}`)}>
                  Open
                </button>
              ) : (
                <button type="button" className="btn-primary" disabled={busyId === item.id} onClick={() => join(item)}>
                  Join
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
