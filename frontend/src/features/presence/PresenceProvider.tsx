import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { PresenceContext } from './PresenceContext'
import type { PresenceEntry, PresenceStatus } from './types'

/** Browser events that count as the user interacting with this tab. */
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'wheel', 'touchstart', 'scroll'] as const

/** Server push events other features can subscribe to (see backend RealtimeEvents). */
const PUSH_EVENTS = [
  'FriendsChanged',
  'MessageReceived',
  'MessageEdited',
  'MessageDeleted',
  'ConversationsChanged',
  'RoomsChanged',
] as const

/** Events that may have been missed while the connection was down: subscribers refetch (payload null). */
const REFETCH_AFTER_RECONNECT = ['FriendsChanged', 'ConversationsChanged', 'RoomsChanged']

/**
 * Minimum gap between activity reports from one tab. The server marks a tab idle after 60 s without a
 * report, so this only needs to be well below that; a report after being idle is sent immediately.
 */
const ACTIVITY_THROTTLE_MS = 3000

type Handler = (payload: unknown) => void

/**
 * The app-wide SignalR connection (one per browser tab). It reports user activity for presence, and
 * relays server push events (friends, messages, rooms) to subscribers.
 * The server combines all tabs of a user: online if any tab is active, AFK if all are idle, offline when none is open.
 */
export default function PresenceProvider({ children }: { children: ReactNode }) {
  const [entries, setEntries] = useState<Record<string, PresenceEntry>>({})
  const [connected, setConnected] = useState(false)
  const listeners = useRef(new Map<string, Set<Handler>>())

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/presence')
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build()

    let lastSent = 0
    const reportActivity = (force = false) => {
      const now = Date.now()
      if (connection.state !== HubConnectionState.Connected) return
      if (!force && now - lastSent < ACTIVITY_THROTTLE_MS) return
      lastSent = now
      connection.invoke('Activity').catch(() => {})
    }

    const loadSnapshot = async () => {
      const snapshot = await connection.invoke<PresenceEntry[]>('GetSnapshot')
      setEntries(Object.fromEntries(snapshot.map((entry) => [entry.userId, entry])))
    }

    connection.on('PresenceChanged', (change: PresenceEntry) => {
      setEntries((current) => {
        const next = { ...current }
        if (change.status === 'offline') delete next[change.userId]
        else next[change.userId] = change
        return next
      })
    })

    for (const event of PUSH_EVENTS) {
      connection.on(event, (payload: unknown) => {
        listeners.current.get(event)?.forEach((handler) => handler(payload))
      })
    }

    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => {
      setConnected(true)
      reportActivity(true)
      void loadSnapshot()
      for (const event of REFETCH_AFTER_RECONNECT) {
        listeners.current.get(event)?.forEach((handler) => handler(null))
      }
    })
    connection.onclose(() => setConnected(false))

    const onActivity = () => reportActivity()
    const onVisible = () => {
      if (document.visibilityState === 'visible') reportActivity(true)
    }
    for (const name of ACTIVITY_EVENTS) window.addEventListener(name, onActivity, { passive: true })
    window.addEventListener('focus', onActivity)
    document.addEventListener('visibilitychange', onVisible)

    const started = connection
      .start()
      .then(async () => {
        setConnected(true)
        lastSent = Date.now() // connecting counts as activity on the server
        await loadSnapshot()
      })
      .catch(() => {
        // Presence is non-critical; a page reload reconnects.
      })

    return () => {
      for (const name of ACTIVITY_EVENTS) window.removeEventListener(name, onActivity)
      window.removeEventListener('focus', onActivity)
      document.removeEventListener('visibilitychange', onVisible)
      void started.finally(() => connection.stop())
    }
  }, [])

  const statusOf = useCallback(
    (userId: string): PresenceStatus => entries[userId]?.status ?? 'offline',
    [entries],
  )

  const subscribe = useCallback((event: string, handler: Handler) => {
    let set = listeners.current.get(event)
    if (!set) {
      set = new Set()
      listeners.current.set(event, set)
    }
    set.add(handler)
    return () => {
      set.delete(handler)
    }
  }, [])

  const active = useMemo(
    () => Object.values(entries).sort((a, b) => a.username.localeCompare(b.username)),
    [entries],
  )

  const value = useMemo(
    () => ({ connected, active, statusOf, subscribe }),
    [connected, active, statusOf, subscribe],
  )
  return <PresenceContext.Provider value={value}>{children}</PresenceContext.Provider>
}
