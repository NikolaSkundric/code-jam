export interface User {
  id: string
  email: string
  username: string
}

export interface SessionInfo {
  id: string
  userAgent: string
  ipAddress: string
  createdAt: string
  lastSeenAt: string
  isCurrent: boolean
}
