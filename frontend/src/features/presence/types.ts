export type PresenceStatus = 'online' | 'afk' | 'offline'

export interface PresenceEntry {
  userId: string
  username: string
  status: PresenceStatus
}
