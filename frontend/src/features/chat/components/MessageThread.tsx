import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react'
import { useAuth } from '@/features/auth'
import { usePresence } from '@/features/presence'
import { ApiError } from '@/lib/http'
import { chatApi } from '../api/chatApi'
import type { Message, MessageDeletedEvent, PendingAttachment } from '../types'
import MessageComposer from './MessageComposer'
import MessageItem from './MessageItem'

const NEAR_EDGE_PX = 80
const dayFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'full' })

function sameDay(a: string, b: string) {
  return new Date(a).toDateString() === new Date(b).toDateString()
}

interface MessageThreadProps {
  /** Chat id (for a room this is the room id). */
  chatId: string
  title: string
  subtitle?: string
  /** False for a frozen chat: history only. */
  canWrite: boolean
  /** Room owner/admins may delete other people's messages. */
  canModerate?: boolean
  readOnlyNotice?: string
  /** Called after the chat was marked as read (to refresh unread counters). */
  onRead?: () => void
}

/**
 * Message history with infinite scroll upwards, live updates and the composer; used for personal
 * chats and rooms alike. Mount with key=chatId.
 */
export default function MessageThread({
  chatId,
  title,
  subtitle,
  canWrite,
  canModerate = false,
  readOnlyNotice,
  onRead,
}: MessageThreadProps) {
  const { user } = useAuth()
  const { subscribe } = usePresence()

  const [messages, setMessages] = useState<Message[]>([])
  const [hasMore, setHasMore] = useState(false)
  const [loading, setLoading] = useState(true)
  const [loadingOlder, setLoadingOlder] = useState(false)
  const [replyTo, setReplyTo] = useState<Message | null>(null)
  const [error, setError] = useState('')

  const listRef = useRef<HTMLDivElement>(null)
  const stickToBottom = useRef(true) // autoscroll only while the user is at the bottom (TASK.md 4.2)
  const restoreHeight = useRef<number | null>(null) // scrollHeight before older messages were prepended
  const onReadRef = useRef(onRead)

  useEffect(() => {
    onReadRef.current = onRead
  }, [onRead])

  const markRead = useCallback(() => {
    if (document.visibilityState !== 'visible') return
    chatApi
      .markRead(chatId)
      .then(() => onReadRef.current?.())
      .catch(() => {})
  }, [chatId])

  // Initial load.
  useEffect(() => {
    let cancelled = false
    chatApi
      .messages(chatId)
      .then((page) => {
        if (cancelled) return
        setMessages(page.messages)
        setHasMore(page.hasMore)
        markRead()
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiError ? err.message : 'Could not load messages.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [chatId, markRead])

  // Live updates for this chat.
  useEffect(() => {
    const offReceived = subscribe('MessageReceived', (payload) => {
      const message = payload as Message
      if (message.chatId !== chatId) return
      setMessages((current) => (current.some((m) => m.id === message.id) ? current : [...current, message]))
      if (message.authorId !== user?.id) markRead()
    })
    const offEdited = subscribe('MessageEdited', (payload) => {
      const message = payload as Message
      if (message.chatId !== chatId) return
      setMessages((current) => current.map((m) => (m.id === message.id ? message : m)))
    })
    const offDeleted = subscribe('MessageDeleted', (payload) => {
      const event = payload as MessageDeletedEvent
      if (event.chatId !== chatId) return
      setMessages((current) =>
        current
          .filter((m) => m.id !== event.messageId)
          .map((m) =>
            m.replyTo?.id === event.messageId
              ? { ...m, replyTo: { ...m.replyTo, text: null, deleted: true } }
              : m,
          ),
      )
      setReplyTo((current) => (current?.id === event.messageId ? null : current))
    })
    window.addEventListener('focus', markRead)
    return () => {
      offReceived()
      offEdited()
      offDeleted()
      window.removeEventListener('focus', markRead)
    }
  }, [subscribe, chatId, user?.id, markRead])

  // Keep the scroll position right after the list changes.
  useLayoutEffect(() => {
    const el = listRef.current
    if (!el) return
    if (restoreHeight.current !== null) {
      el.scrollTop = el.scrollHeight - restoreHeight.current
      restoreHeight.current = null
    } else if (stickToBottom.current) {
      el.scrollTop = el.scrollHeight
    }
  }, [messages])

  async function loadOlder() {
    const el = listRef.current
    if (!el || loadingOlder || !hasMore || messages.length === 0) return
    setLoadingOlder(true)
    try {
      const page = await chatApi.messages(chatId, messages[0].id)
      restoreHeight.current = el.scrollHeight
      setMessages((current) => [...page.messages, ...current.filter((m) => !page.messages.some((p) => p.id === m.id))])
      setHasMore(page.hasMore)
    } catch {
      // try again on the next scroll
    } finally {
      setLoadingOlder(false)
    }
  }

  function onScroll() {
    const el = listRef.current
    if (!el) return
    stickToBottom.current = el.scrollHeight - el.scrollTop - el.clientHeight < NEAR_EDGE_PX
    if (el.scrollTop < NEAR_EDGE_PX) void loadOlder()
  }

  // An image finishing to load can push the content down: keep the bottom in view if we were there.
  function onMediaLoad() {
    const el = listRef.current
    if (el && stickToBottom.current) el.scrollTop = el.scrollHeight
  }

  async function handleSend(text: string, attachments: PendingAttachment[]) {
    try {
      const message = await chatApi.send(chatId, { text, replyToMessageId: replyTo?.id, attachments })
      stickToBottom.current = true
      setMessages((current) => (current.some((m) => m.id === message.id) ? current : [...current, message]))
      setReplyTo(null)
    } catch (err) {
      throw new Error(
        err instanceof ApiError
          ? (err.fieldErrors.text ?? err.fieldErrors.files ?? err.message)
          : 'Could not send the message.',
        { cause: err },
      )
    }
  }

  async function handleEdit(message: Message, text: string) {
    try {
      const updated = await chatApi.edit(chatId, message.id, text)
      setMessages((current) => current.map((m) => (m.id === updated.id ? updated : m)))
    } catch (err) {
      setError(err instanceof ApiError ? (err.fieldErrors.text ?? err.message) : 'Could not edit the message.')
      throw err
    }
  }

  async function handleDelete(message: Message) {
    try {
      await chatApi.remove(chatId, message.id)
      setMessages((current) => current.filter((m) => m.id !== message.id))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not delete the message.')
    }
  }

  return (
    <section className="thread" aria-label={title}>
      <header className="thread-header">
        <h2>{title}</h2>
        {subtitle && <span className="thread-subtitle">{subtitle}</span>}
        {!canWrite && <span className="thread-frozen">Read-only</span>}
      </header>

      {!canWrite && readOnlyNotice && (
        <p className="thread-banner" role="status">
          {readOnlyNotice}
        </p>
      )}
      {error && (
        <p className="form-error thread-error" role="alert">
          {error}
        </p>
      )}

      <div className="thread-messages" ref={listRef} onScroll={onScroll}>
        {loadingOlder && <p className="thread-note">Loading older messages…</p>}
        {!loading && !hasMore && messages.length > 0 && <p className="thread-note">Beginning of the conversation</p>}
        {loading && <p className="thread-note">Loading…</p>}
        {!loading && messages.length === 0 && <p className="thread-note">No messages yet. Say hello!</p>}

        <div className="message-list" role="log" aria-live="polite">
          {messages.map((message, index) => (
            <div key={message.id} className="message-wrap">
              {(index === 0 || !sameDay(messages[index - 1].createdAt, message.createdAt)) && (
                <p className="day-divider">{dayFormat.format(new Date(message.createdAt))}</p>
              )}
              <MessageItem
                message={message}
                mine={message.authorId === user?.id}
                canWrite={canWrite}
                canModerate={canModerate}
                onReply={setReplyTo}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onMediaLoad={onMediaLoad}
              />
            </div>
          ))}
        </div>
      </div>

      <MessageComposer
        disabled={!canWrite}
        replyTo={replyTo}
        onCancelReply={() => setReplyTo(null)}
        onSend={handleSend}
      />
    </section>
  )
}
