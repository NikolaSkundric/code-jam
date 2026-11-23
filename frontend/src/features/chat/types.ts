export interface ReplyPreview {
  id: string
  authorUsername: string
  /** Null when the original message was deleted. */
  text: string | null
  deleted: boolean
}

export interface Attachment {
  id: string
  /** Original file name. */
  fileName: string
  contentType: string
  sizeBytes: number
  /** A verified raster image (shown inline); everything else is a plain file download. */
  isImage: boolean
  comment: string | null
}

export interface Message {
  id: string
  chatId: string
  authorId: string
  authorUsername: string
  text: string
  replyTo: ReplyPreview | null
  attachments: Attachment[]
  createdAt: string
  editedAt: string | null
}

export interface MessagePage {
  messages: Message[]
  /** Older messages exist. */
  hasMore: boolean
}

export interface Conversation {
  chatId: string
  /** The other participant. */
  userId: string
  username: string
  lastMessageText: string | null
  lastMessageAt: string | null
  unreadCount: number
  /** False when the chat is frozen (not friends any more, or banned): history only. */
  canWrite: boolean
}

export interface MessageDeletedEvent {
  chatId: string
  messageId: string
}

/** A file chosen in the composer, not sent yet. */
export interface PendingAttachment {
  id: string
  file: File
  comment: string
  /** Object URL for image previews. */
  previewUrl?: string
}

/** Server limits (TASK.md 2.5.2, 3.4). */
export const MAX_MESSAGE_BYTES = 3 * 1024
export const MAX_FILE_BYTES = 20 * 1024 * 1024
export const MAX_IMAGE_BYTES = 3 * 1024 * 1024
export const MAX_FILES_PER_MESSAGE = 5
export const IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/gif', 'image/webp']

export function attachmentUrl(id: string) {
  return `/api/attachments/${id}`
}
