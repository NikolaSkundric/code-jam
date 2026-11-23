export interface Friend {
  userId: string
  username: string
  friendsSince: string
}

/** A pending friend request; userId/username describe the other person. */
export interface FriendRequest {
  id: string
  userId: string
  username: string
  message: string | null
  createdAt: string
}

export interface FriendRequests {
  incoming: FriendRequest[]
  outgoing: FriendRequest[]
}

/** A user the current user has banned. */
export interface BannedUser {
  userId: string
  username: string
  bannedAt: string
}
