import { createContext } from 'react'
import type { IncomingInvitation, RoomSummary } from './types'

export interface RoomsContextValue {
  /** Rooms the user is a member of. */
  rooms: RoomSummary[]
  /** Pending invitations to rooms. */
  invitations: IncomingInvitation[]
  /** Unread messages across all rooms. */
  unreadTotal: number
  loading: boolean
  reload: () => Promise<void>
}

export const RoomsContext = createContext<RoomsContextValue | null>(null)
