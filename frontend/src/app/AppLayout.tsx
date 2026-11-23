import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import TopBar from '@/components/ui/TopBar'
import { useAuth } from '@/features/auth'
import { ChatProvider, useChat } from '@/features/chat'
import { FriendsProvider, useFriends } from '@/features/friends'
import { PresenceDot, PresenceProvider, usePresence } from '@/features/presence'
import { RoomsProvider, useRooms } from '@/features/rooms'

/** Own presence next to the user name (offline until the connection is up). */
function CurrentUser() {
  const { user } = useAuth()
  const { statusOf, connected } = usePresence()
  if (!user) return null

  return (
    <span className="nav-user">
      <PresenceDot status={connected ? statusOf(user.id) : 'offline'} showLabel />
      <span>{user.username}</span>
    </span>
  )
}

/** Chats: unread messages in personal chats and rooms, plus pending room invitations. */
function ChatsLink() {
  const { unreadTotal } = useChat()
  const { unreadTotal: roomsUnread, invitations } = useRooms()
  const total = unreadTotal + roomsUnread + invitations.length
  return (
    <NavLink to="/messages" className="nav-link">
      Chats
      {total > 0 && (
        <span className="nav-badge" aria-label={`${total} unread or pending`}>
          {total}
        </span>
      )}
    </NavLink>
  )
}

function ContactsLink() {
  const { incoming } = useFriends()
  return (
    <NavLink to="/contacts" className="nav-link">
      Contacts
      {incoming.length > 0 && (
        <span className="nav-badge" aria-label={`${incoming.length} pending friend requests`}>
          {incoming.length}
        </span>
      )}
    </NavLink>
  )
}

/** Layout for signed-in pages: top menu and the page below. */
export default function AppLayout() {
  const { signOut } = useAuth()
  const navigate = useNavigate()

  async function handleSignOut() {
    await signOut()
    navigate('/login', { replace: true })
  }

  return (
    <PresenceProvider>
      <FriendsProvider>
        <ChatProvider>
          <RoomsProvider>
            <div className="app-page">
              <TopBar logoTo="/">
                <ChatsLink />
                <ContactsLink />
                <NavLink to="/sessions" className="nav-link">
                  Sessions
                </NavLink>
                <NavLink to="/settings" className="nav-link">
                  Settings
                </NavLink>
                <CurrentUser />
                <button type="button" className="nav-link" onClick={handleSignOut}>
                  Sign out
                </button>
              </TopBar>
              <main>
                <Outlet />
              </main>
            </div>
          </RoomsProvider>
        </ChatProvider>
      </FriendsProvider>
    </PresenceProvider>
  )
}
