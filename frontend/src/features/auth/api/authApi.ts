import { deleteJson, getJson, postJson } from '@/lib/http'
import type { SessionInfo, User } from '../types'

export interface LoginRequest {
  email: string
  password: string
  keepSignedIn: boolean
}

export interface RegisterRequest {
  email: string
  username: string
  password: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface DeleteAccountRequest {
  password: string
}

export const authApi = {
  login: (request: LoginRequest) => postJson<User>('/api/auth/login', request),
  register: (request: RegisterRequest) => postJson<User>('/api/auth/register', request),
  logout: () => postJson('/api/auth/logout'),
  me: () => getJson<User>('/api/auth/me'),
  forgotPassword: (request: ForgotPasswordRequest) => postJson('/api/auth/forgot-password', request),
  resetPassword: (request: ResetPasswordRequest) => postJson('/api/auth/reset-password', request),
  changePassword: (request: ChangePasswordRequest) => postJson('/api/auth/change-password', request),
  deleteAccount: (request: DeleteAccountRequest) => deleteJson('/api/auth/account', request),
  getSessions: () => getJson<SessionInfo[]>('/api/auth/sessions'),
  revokeSession: (sessionId: string) => deleteJson(`/api/auth/sessions/${sessionId}`),
}
