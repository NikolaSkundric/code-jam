import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import './topbar.css'

interface TopBarProps {
  /** Where the logo links to. */
  logoTo: string
  children?: ReactNode
}

/** Shared top bar: logo on the left, navigation passed as children on the right. */
export default function TopBar({ logoTo, children }: TopBarProps) {
  return (
    <header className="topbar">
      <Link to={logoTo} className="logo">
        <span className="logo-mark" aria-hidden="true">
          #
        </span>
        Chat
      </Link>
      <nav className="topbar-nav" aria-label="Main">
        {children}
      </nav>
    </header>
  )
}
