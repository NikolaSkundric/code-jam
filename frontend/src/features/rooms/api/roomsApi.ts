import { deleteJson, getJson, postJson, putJson } from '@/lib/http'
import type {
  IncomingInvitation,
  RoomBan,
  RoomCatalogItem,
  RoomDetails,
  RoomInput,
  RoomInvitation,
  RoomSummary,
} from '../types'

export const roomsApi = {
  mine: () => getJson<RoomSummary[]>('/api/rooms'),
  catalog: (search?: string) =>
    getJson<RoomCatalogItem[]>(`/api/rooms/catalog${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  get: (roomId: string) => getJson<RoomDetails>(`/api/rooms/${roomId}`),
  create: (input: RoomInput) => postJson<RoomDetails>('/api/rooms', input),
  update: (roomId: string, input: RoomInput) => putJson<RoomDetails>(`/api/rooms/${roomId}`, input),
  remove: (roomId: string) => deleteJson(`/api/rooms/${roomId}`),

  join: (roomId: string) => postJson(`/api/rooms/${roomId}/join`),
  leave: (roomId: string) => postJson(`/api/rooms/${roomId}/leave`),

  setAdmin: (roomId: string, userId: string, isAdmin: boolean) =>
    putJson(`/api/rooms/${roomId}/members/${userId}/role`, { isAdmin }),
  bans: (roomId: string) => getJson<RoomBan[]>(`/api/rooms/${roomId}/bans`),
  ban: (roomId: string, userId: string) => postJson(`/api/rooms/${roomId}/bans`, { userId }),
  unban: (roomId: string, userId: string) => deleteJson(`/api/rooms/${roomId}/bans/${userId}`),

  invitations: (roomId: string) => getJson<RoomInvitation[]>(`/api/rooms/${roomId}/invitations`),
  invite: (roomId: string, username: string) => postJson(`/api/rooms/${roomId}/invitations`, { username }),
  cancelInvitation: (roomId: string, invitationId: string) =>
    deleteJson(`/api/rooms/${roomId}/invitations/${invitationId}`),

  myInvitations: () => getJson<IncomingInvitation[]>('/api/invitations'),
  acceptInvitation: (id: string) => postJson(`/api/invitations/${id}/accept`),
  declineInvitation: (id: string) => postJson(`/api/invitations/${id}/decline`),
}
