export type RoomVisibility = 'public' | 'private'
export type RoomRole = 'owner' | 'admin' | 'member'

export interface RoomSummary {
  id: string
  name: string
  description: string
  visibility: RoomVisibility
  memberCount: number
  myRole: RoomRole
  unreadCount: number
}

export interface RoomCatalogItem {
  id: string
  name: string
  description: string
  memberCount: number
  isMember: boolean
}

export interface RoomMember {
  userId: string
  username: string
  role: RoomRole
  joinedAt: string
}

export interface RoomDetails {
  id: string
  name: string
  description: string
  visibility: RoomVisibility
  ownerId: string
  ownerUsername: string
  myRole: RoomRole
  members: RoomMember[]
}

export interface RoomBan {
  userId: string
  username: string
  bannedById: string
  bannedByUsername: string
  bannedAt: string
}

/** A pending invitation as seen by room admins. */
export interface RoomInvitation {
  id: string
  userId: string
  username: string
  invitedByUsername: string
  createdAt: string
}

/** An invitation received by the current user. */
export interface IncomingInvitation {
  id: string
  roomId: string
  roomName: string
  invitedByUsername: string
  createdAt: string
}

export interface RoomInput {
  name: string
  description: string
  visibility: RoomVisibility
}

export const isAdminRole = (role: RoomRole) => role === 'owner' || role === 'admin'
