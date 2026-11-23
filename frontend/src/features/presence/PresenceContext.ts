import { createContext } from 'react'
import type { PresenceEntry, PresenceStatus } from './types'

export interface PresenceContextValue {
  /** True while the SignalR connection is up. */
  connected: boolean
  /** Users that are currently online or AFK (sorted by name). */
  active: PresenceEntry[]
  statusOf: (userId: string) => PresenceStatus
  /**
   * Subscribes to a server push event (e.g. 'FriendsChanged', 'MessageReceived') on the app-wide
   * SignalR connection. Returns an unsubscribe function.
   */
  subscribe: (event: string, handler: (payload: unknown) => void) => () => void
}

export const PresenceContext = createContext<PresenceContextValue | null>(null)
