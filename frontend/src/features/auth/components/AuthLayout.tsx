import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import TopBar from '@/components/ui/TopBar'
import './auth-layout.css'

interface AuthLayoutProps {
  title: string
  children: ReactNode
}

/** Top bar (logo + Sign in / Register) and a centered card, as in the wireframes. */
export default function AuthLayout({ title, children }: AuthLayoutProps) {
  return (
    <div className="auth-page">
      <TopBar logoTo="/login">
        <NavLink to="/login" className="nav-link">
          Sign in
        </NavLink>
        <NavLink to="/register" className="nav-link">
          Register
        </NavLink>
      </TopBar>

      <main className="auth-main">
        <section className="auth-card">
          <h1>{title}</h1>
          {children}
        </section>
      </main>
    </div>
  )
}
