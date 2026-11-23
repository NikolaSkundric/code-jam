import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import TopBar from '@/components/ui/TopBar'
import { useAuth } from '@/features/auth'
import { ChatProvider, useChat } from '@/features/chat'
import { FriendsProvider, useFriends } from '@/features/friends'
import { PresenceProvider } from '@/features/presence'
import { RoomsProvider, useRooms } from '@/features/rooms'
import ProfileMenu from './ProfileMenu'

/** Chats: unread messages in personal chats and rooms. */
function ChatsLink() {
  const { unreadTotal } = useChat()
  const { unreadTotal: roomsUnread } = useRooms()
  const total = unreadTotal + roomsUnread
  return (
    <NavLink to="/messages" className="nav-link">
      Chats
      {total > 0 && (
        <span className="nav-badge" aria-label={`${total} unread messages`}>
          {total}
        </span>
      )}
    </NavLink>
  )
}

function PrivateRoomsLink() {
  const { invitations } = useRooms()
  return (
    <NavLink to="/rooms/private" className="nav-link">
      Private Rooms
      {invitations.length > 0 && (
        <span className="nav-badge" aria-label={`${invitations.length} pending invitations`}>
          {invitations.length}
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

/** Layout for signed-in pages. Top menu as in the wireframe: Public Rooms, Private Rooms, Contacts, Sessions, Profile, Sign out. */
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
                <NavLink to="/rooms" end className="nav-link">
                  Public Rooms
                </NavLink>
                <PrivateRoomsLink />
                <ContactsLink />
                <NavLink to="/sessions" className="nav-link">
                  Sessions
                </NavLink>
                <ProfileMenu />
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
