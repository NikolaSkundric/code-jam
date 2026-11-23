import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { authApi } from './api/authApi'
import { AuthContext } from './AuthContext'
import type { User } from './types'

/** Restores the signed-in user from the HttpOnly cookie on load (GET /api/auth/me). */
export default function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    authApi
      .me()
      .then((me) => {
        if (!cancelled) setUser(me)
      })
      .catch(() => {
        // 401 or network error: treated as signed out
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const signOut = useCallback(async () => {
    try {
      await authApi.logout()
    } catch {
      // the local state is cleared even if the request fails
    }
    setUser(null)
  }, [])

  const value = useMemo(() => ({ user, loading, setUser, signOut }), [user, loading, signOut])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
