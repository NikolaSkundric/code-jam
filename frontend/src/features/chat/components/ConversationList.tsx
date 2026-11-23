import { NavLink, useNavigate } from 'react-router-dom'
import { useFriends } from '@/features/friends'
import { PresenceDot, usePresence } from '@/features/presence'
import { useChat } from '../useChat'

/** Contacts: personal dialogs (with unread counters) and friends you have not written to yet. */
export default function ConversationList() {
  const { conversations, openWith } = useChat()
  const { friends } = useFriends()
  const { statusOf } = usePresence()
  const navigate = useNavigate()

  const withoutChat = friends.filter((f) => !conversations.some((c) => c.userId === f.userId))

  async function start(friendId: string) {
    try {
      const conversation = await openWith(friendId)
      navigate(`/messages/${conversation.chatId}`)
    } catch {
      // the friends list is refreshed by the server push if the friendship changed
    }
  }

  return (
    <nav className="conversation-list" aria-label="Contacts">
      <ul>
        {conversations.map((c) => (
          <li key={c.chatId}>
            <NavLink to={`/messages/${c.chatId}`} className="conversation">
              <PresenceDot status={statusOf(c.userId)} />
              <span className="conversation-body">
                <span className="conversation-name">{c.username}</span>
                {c.lastMessageText && <span className="conversation-preview">{c.lastMessageText}</span>}
              </span>
              {c.unreadCount > 0 && (
                <span className="nav-badge" aria-label={`${c.unreadCount} unread messages`}>
                  {c.unreadCount}
                </span>
              )}
            </NavLink>
          </li>
        ))}
        {withoutChat.map((f) => (
          <li key={f.userId}>
            <button type="button" className="conversation conversation-new" onClick={() => start(f.userId)}>
              <PresenceDot status={statusOf(f.userId)} />
              <span className="conversation-body">
                <span className="conversation-name">{f.username}</span>
                <span className="conversation-preview">Start a conversation</span>
              </span>
            </button>
          </li>
        ))}
      </ul>
      {conversations.length === 0 && withoutChat.length === 0 && (
        <p className="thread-note">Add friends on the Contacts page to start chatting.</p>
      )}
    </nav>
  )
}
