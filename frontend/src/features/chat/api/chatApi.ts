import { deleteJson, getJson, postForm, postJson, putJson } from '@/lib/http'
import type { Conversation, Message, MessagePage, PendingAttachment } from '../types'

export interface SendInput {
  text: string
  replyToMessageId?: string
  attachments?: PendingAttachment[]
}

export const chatApi = {
  conversations: () => getJson<Conversation[]>('/api/personal-chats'),
  open: (userId: string) => postJson<Conversation>('/api/personal-chats', { userId }),

  messages: (chatId: string, before?: string, limit = 30) =>
    getJson<MessagePage>(
      `/api/chats/${chatId}/messages?limit=${limit}${before ? `&before=${before}` : ''}`,
    ),

  /** Always multipart so text and files travel in one request. */
  send: (chatId: string, { text, replyToMessageId, attachments = [] }: SendInput) => {
    const form = new FormData()
    form.append('text', text)
    if (replyToMessageId) form.append('replyToMessageId', replyToMessageId)
    for (const attachment of attachments) {
      form.append('files', attachment.file, attachment.file.name)
      form.append('comments', attachment.comment)
    }
    return postForm<Message>(`/api/chats/${chatId}/messages`, form)
  },
  edit: (chatId: string, messageId: string, text: string) =>
    putJson<Message>(`/api/chats/${chatId}/messages/${messageId}`, { text }),
  remove: (chatId: string, messageId: string) => deleteJson(`/api/chats/${chatId}/messages/${messageId}`),
  markRead: (chatId: string) => postJson(`/api/chats/${chatId}/read`),
}
