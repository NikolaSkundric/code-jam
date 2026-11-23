import { useEffect, useRef, useState } from 'react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '@/features/auth'
import { PresenceDot, usePresence } from '@/features/presence'
import './profile-menu.css'

/** Top menu "Profile ▼": your name and status, and a link to the settings (password, delete account). */
export default function ProfileMenu() {
  const { user } = useAuth()
  const { statusOf, connected } = usePresence()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) setOpen(false)
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  if (!user) return null
  const status = connected ? statusOf(user.id) : 'offline'

  return (
    <div className="profile-menu" ref={rootRef}>
      <button
        type="button"
        className="nav-link"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
      >
        <PresenceDot status={status} /> Profile ▼
      </button>
      {open && (
        <div className="profile-dropdown" role="menu">
          <p className="profile-summary">
            <strong>{user.username}</strong>
            <span className="muted">{user.email}</span>
            <PresenceDot status={status} showLabel />
          </p>
          <NavLink to="/settings" role="menuitem" className="profile-item" onClick={() => setOpen(false)}>
            Settings
          </NavLink>
          {user.isAdmin && (
            <NavLink to="/admin/jabber" role="menuitem" className="profile-item" onClick={() => setOpen(false)}>
              Jabber admin
            </NavLink>
          )}
        </div>
      )}
    </div>
  )
}
