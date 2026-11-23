import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { usePresence } from '@/features/presence'
import { friendsApi } from './api/friendsApi'
import { FriendsContext } from './FriendsContext'
import type { BannedUser, Friend, FriendRequest } from './types'

/** Loads friends, pending requests and bans, and refreshes them when the server reports a change. */
export default function FriendsProvider({ children }: { children: ReactNode }) {
  const { subscribe } = usePresence()
  const [friends, setFriends] = useState<Friend[]>([])
  const [incoming, setIncoming] = useState<FriendRequest[]>([])
  const [outgoing, setOutgoing] = useState<FriendRequest[]>([])
  const [banned, setBanned] = useState<BannedUser[]>([])
  const [loading, setLoading] = useState(true)

  const reload = useCallback(async () => {
    try {
      const [friendList, requests, bans] = await Promise.all([
        friendsApi.list(),
        friendsApi.requests(),
        friendsApi.bans(),
      ])
      setFriends(friendList)
      setIncoming(requests.incoming)
      setOutgoing(requests.outgoing)
      setBanned(bans)
    } catch {
      // keep the previous data; the next change event or page action reloads again
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial data fetch
    void reload()
    return subscribe('FriendsChanged', () => void reload())
  }, [reload, subscribe])

  const value = useMemo(
    () => ({ friends, incoming, outgoing, banned, loading, reload }),
    [friends, incoming, outgoing, banned, loading, reload],
  )
  return <FriendsContext.Provider value={value}>{children}</FriendsContext.Provider>
}
