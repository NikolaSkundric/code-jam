import { useContext } from 'react'
import { RoomsContext } from './RoomsContext'

export function useRooms() {
  const context = useContext(RoomsContext)
  if (!context) throw new Error('useRooms must be used inside <RoomsProvider>')
  return context
}
