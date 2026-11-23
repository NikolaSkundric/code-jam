export interface User {
  id: string
  email: string
  username: string
  /** May open the administration pages. */
  isAdmin: boolean
}

export interface SessionInfo {
  id: string
  userAgent: string
  ipAddress: string
  createdAt: string
  lastSeenAt: string
  isCurrent: boolean
}
