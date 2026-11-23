import { useParams } from 'react-router-dom'
import MessageThread from '../components/MessageThread'
import { useChat } from '../useChat'
import './chat.css'
import './attachments.css'

/** A personal chat (or the empty state). The sidebar comes from the surrounding chat layout. */
export default function MessagesPage() {
  const { chatId } = useParams()
  const { conversations, loading, reload } = useChat()
  const conversation = conversations.find((c) => c.chatId === chatId)

  return (
    <div className="chat-main">
      {conversation ? (
        <MessageThread
          key={conversation.chatId}
          chatId={conversation.chatId}
          title={conversation.username}
          canWrite={conversation.canWrite}
          readOnlyNotice="You can no longer send messages in this chat (not friends, or a ban is active). The history stays visible."
          onRead={reload}
        />
      ) : (
        <p className="thread-note thread-empty">
          {chatId && !loading ? 'Conversation not found.' : 'Select a chat to start messaging.'}
        </p>
      )}
    </div>
  )
}
