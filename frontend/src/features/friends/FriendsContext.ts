import { createContext } from 'react'
import type { BannedUser, Friend, FriendRequest } from './types'

export interface FriendsContextValue {
  friends: Friend[]
  incoming: FriendRequest[]
  outgoing: FriendRequest[]
  /** Users the current user has banned. */
  banned: BannedUser[]
  loading: boolean
  /** Reloads friends, requests and bans from the server. */
  reload: () => Promise<void>
}

export const FriendsContext = createContext<FriendsContextValue | null>(null)
