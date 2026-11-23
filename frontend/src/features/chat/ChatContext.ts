import { createContext } from 'react'
import type { Conversation } from './types'

export interface ChatContextValue {
  conversations: Conversation[]
  /** Total unread messages across all conversations. */
  unreadTotal: number
  loading: boolean
  reload: () => Promise<void>
  /** Opens (creating if needed) the dialog with a friend. */
  openWith: (friendUserId: string) => Promise<Conversation>
}

export const ChatContext = createContext<ChatContextValue | null>(null)
