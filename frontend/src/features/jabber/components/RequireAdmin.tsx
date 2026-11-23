import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth'

/** Route guard: only users with the admin flag may open the nested routes. */
export default function RequireAdmin() {
  const { user } = useAuth()
  return user?.isAdmin ? <Outlet /> : <Navigate to="/" replace />
}
