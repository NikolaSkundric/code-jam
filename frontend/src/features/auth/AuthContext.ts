import { createContext } from 'react'
import type { User } from './types'

export interface AuthContextValue {
  /** Signed-in user, or null when signed out. */
  user: User | null
  /** True until the initial /me check finishes. */
  loading: boolean
  setUser: (user: User | null) => void
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
