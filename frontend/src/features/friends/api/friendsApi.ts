import { deleteJson, getJson, postJson } from '@/lib/http'
import type { BannedUser, Friend, FriendRequests } from '../types'

export interface SendFriendRequest {
  username: string
  message?: string
}

export const friendsApi = {
  list: () => getJson<Friend[]>('/api/friends'),
  remove: (friendId: string) => deleteJson(`/api/friends/${friendId}`),
  requests: () => getJson<FriendRequests>('/api/friends/requests'),
  sendRequest: (request: SendFriendRequest) => postJson('/api/friends/requests', request),
  accept: (requestId: string) => postJson(`/api/friends/requests/${requestId}/accept`),
  decline: (requestId: string) => postJson(`/api/friends/requests/${requestId}/decline`),
  cancel: (requestId: string) => deleteJson(`/api/friends/requests/${requestId}`),

  bans: () => getJson<BannedUser[]>('/api/bans'),
  ban: (username: string) => postJson('/api/bans', { username }),
  unban: (userId: string) => deleteJson(`/api/bans/${userId}`),
}
