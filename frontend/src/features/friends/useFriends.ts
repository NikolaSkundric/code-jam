import { useContext } from 'react'
import { FriendsContext } from './FriendsContext'

export function useFriends() {
  const context = useContext(FriendsContext)
  if (!context) throw new Error('useFriends must be used inside <FriendsProvider>')
  return context
}
