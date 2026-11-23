import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { usePresence } from '@/features/presence'
import { chatApi } from './api/chatApi'
import { ChatContext } from './ChatContext'
import type { Conversation } from './types'

/** Conversation list + unread counters; refreshed on every relevant server push. */
export default function ChatProvider({ children }: { children: ReactNode }) {
  const { subscribe } = usePresence()
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [loading, setLoading] = useState(true)
  const timer = useRef<number>(undefined)

  const reload = useCallback(async () => {
    try {
      setConversations(await chatApi.conversations())
    } catch {
      // keep the previous list
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void reload()

    // Coalesce bursts of events into one refetch.
    const scheduleReload = () => {
      window.clearTimeout(timer.current)
      timer.current = window.setTimeout(() => void reload(), 150)
    }
    const unsubscribers = [
      'MessageReceived',
      'MessageEdited',
      'MessageDeleted',
      'ConversationsChanged',
      'FriendsChanged',
    ].map((event) => subscribe(event, scheduleReload))

    return () => {
      window.clearTimeout(timer.current)
      unsubscribers.forEach((unsubscribe) => unsubscribe())
    }
  }, [reload, subscribe])

  const openWith = useCallback(
    async (friendUserId: string) => {
      const conversation = await chatApi.open(friendUserId)
      await reload()
      return conversation
    },
    [reload],
  )

  const unreadTotal = useMemo(() => conversations.reduce((sum, c) => sum + c.unreadCount, 0), [conversations])
  const value = useMemo(
    () => ({ conversations, unreadTotal, loading, reload, openWith }),
    [conversations, unreadTotal, loading, reload, openWith],
  )
  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>
}
