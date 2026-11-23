import { Outlet, useLocation } from 'react-router-dom'
import { ConversationList } from '@/features/chat'
import { RoomsSidebarSection } from '@/features/rooms'
import './chat-layout.css'

/** Chat area (wireframe): sidebar with rooms and contacts on the left, the open chat/room on the right. */
export default function ChatLayout() {
  const { pathname } = useLocation()
  // On phones the sidebar and the open chat are shown one at a time.
  const showSidebarOnly = pathname === '/messages'

  return (
    <div className={`chat-layout${showSidebarOnly ? ' sidebar-only' : ''}`}>
      <aside className="chat-sidebar">
        <RoomsSidebarSection />
        <ConversationList />
      </aside>
      <Outlet />
    </div>
  )
}
