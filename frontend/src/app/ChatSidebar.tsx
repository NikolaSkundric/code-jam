import { useMemo, useState } from 'react'
import { matchPath, useLocation } from 'react-router-dom'
import AccordionSection from '@/components/ui/AccordionSection'
import { ConversationList, useChat } from '@/features/chat'
import { InvitationsBox, RoomActions, RoomList, useRooms } from '@/features/rooms'

type SectionId = 'public' | 'private' | 'contacts'
const ALL_SECTIONS: SectionId[] = ['public', 'private', 'contacts']

/**
 * Rooms (public/private) and contacts as an accordion (TASK.md 4.1.1). Everything is open on the
 * overview; once a room or chat is entered the list compacts to the group that contains it.
 */
export default function ChatSidebar() {
  const { pathname } = useLocation()
  const { rooms } = useRooms()
  const { unreadTotal } = useChat()

  const defaultOpen = useMemo(() => {
    const room = matchPath('/rooms/:roomId', pathname)?.params.roomId
    if (room && room !== 'private') {
      const found = rooms.find((r) => r.id === room)
      return new Set<SectionId>(found ? [found.visibility] : ALL_SECTIONS)
    }
    if (matchPath('/messages/:chatId', pathname)) return new Set<SectionId>(['contacts'])
    return new Set<SectionId>(ALL_SECTIONS)
  }, [pathname, rooms])

  // The user's own toggles apply to the current page only; entering another chat compacts again.
  const [override, setOverride] = useState<{ path: string; open: Set<SectionId> }>()
  const open = override?.path === pathname ? override.open : defaultOpen

  function toggle(id: SectionId) {
    const next = new Set(open)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    setOverride({ path: pathname, open: next })
  }

  const unreadOf = (visibility: 'public' | 'private') =>
    rooms.filter((r) => r.visibility === visibility).reduce((sum, r) => sum + r.unreadCount, 0)

  return (
    <>
      <InvitationsBox />

      <AccordionSection title="Public rooms" badge={unreadOf('public')} open={open.has('public')} onToggle={() => toggle('public')}>
        <RoomList rooms={rooms.filter((r) => r.visibility === 'public')} />
      </AccordionSection>

      <AccordionSection title="Private rooms" badge={unreadOf('private')} open={open.has('private')} onToggle={() => toggle('private')}>
        <RoomList rooms={rooms.filter((r) => r.visibility === 'private')} />
      </AccordionSection>

      <RoomActions />

      <AccordionSection title="Contacts" badge={unreadTotal} open={open.has('contacts')} onToggle={() => toggle('contacts')}>
        <ConversationList />
      </AccordionSection>
    </>
  )
}
