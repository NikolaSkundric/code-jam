import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../useAuth'

/** Route guard: renders nested routes only for a signed-in user. */
export default function RequireAuth() {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return <p className="page-loading">Loading…</p>
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return <Outlet />
}
