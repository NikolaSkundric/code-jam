import { useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import ChatSidebar from './ChatSidebar'
import './chat-layout.css'

const STORAGE_KEY = 'chat.sidebarCollapsed'

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1'
  } catch {
    return false
  }
}

/** Chat area (wireframe): collapsible sidebar with rooms and contacts, the open chat/room next to it. */
export default function ChatLayout() {
  const { pathname } = useLocation()
  const [collapsed, setCollapsed] = useState(readCollapsed)
  // On phones the sidebar and the open chat are shown one at a time.
  const showSidebarOnly = pathname === '/messages'

  function toggle() {
    const next = !collapsed
    setCollapsed(next)
    try {
      localStorage.setItem(STORAGE_KEY, next ? '1' : '0')
    } catch {
      // the preference just is not remembered
    }
  }

  return (
    <div className={`chat-layout${showSidebarOnly ? ' sidebar-only' : ''}${collapsed ? ' collapsed' : ''}`}>
      <aside className="chat-sidebar" aria-label="Rooms and contacts">
        <button
          type="button"
          className="sidebar-toggle"
          onClick={toggle}
          aria-expanded={!collapsed}
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {collapsed ? '»' : '«'}
        </button>
        {!collapsed && <ChatSidebar />}
      </aside>
      <Outlet />
    </div>
  )
}
