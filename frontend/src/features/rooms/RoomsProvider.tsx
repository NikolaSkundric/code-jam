import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { usePresence } from '@/features/presence'
import { roomsApi } from './api/roomsApi'
import { RoomsContext } from './RoomsContext'
import type { IncomingInvitation, RoomSummary } from './types'

/** My rooms, unread counters and invitations; refreshed on every relevant server push. */
export default function RoomsProvider({ children }: { children: ReactNode }) {
  const { subscribe } = usePresence()
  const [rooms, setRooms] = useState<RoomSummary[]>([])
  const [invitations, setInvitations] = useState<IncomingInvitation[]>([])
  const [loading, setLoading] = useState(true)
  const timer = useRef<number>(undefined)

  const reload = useCallback(async () => {
    try {
      const [mine, invites] = await Promise.all([roomsApi.mine(), roomsApi.myInvitations()])
      setRooms(mine)
      setInvitations(invites)
    } catch {
      // keep the previous data
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
      'RoomsChanged',
      'MessageReceived',
      'MessageDeleted',
      'ConversationsChanged',
    ].map((event) => subscribe(event, scheduleReload))

    return () => {
      window.clearTimeout(timer.current)
      unsubscribers.forEach((unsubscribe) => unsubscribe())
    }
  }, [reload, subscribe])

  const unreadTotal = useMemo(() => rooms.reduce((sum, r) => sum + r.unreadCount, 0), [rooms])
  const value = useMemo(
    () => ({ rooms, invitations, unreadTotal, loading, reload }),
    [rooms, invitations, unreadTotal, loading, reload],
  )
  return <RoomsContext.Provider value={value}>{children}</RoomsContext.Provider>
}
