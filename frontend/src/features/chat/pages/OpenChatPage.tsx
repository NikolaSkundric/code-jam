import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useChat } from '../useChat'

/** /messages/with/:userId - opens (creating if needed) the dialog with a friend, then shows it. */
export default function OpenChatPage() {
  const { userId } = useParams()
  const { openWith } = useChat()
  const navigate = useNavigate()
  const [error, setError] = useState('')

  useEffect(() => {
    if (!userId) return
    let cancelled = false
    openWith(userId)
      .then((conversation) => {
        if (!cancelled) navigate(`/messages/${conversation.chatId}`, { replace: true })
      })
      .catch(() => {
        if (!cancelled) setError('You can only chat with your friends.')
      })
    return () => {
      cancelled = true
    }
  }, [userId, openWith, navigate])

  return error ? (
    <p className="form-error" style={{ margin: 24 }} role="alert">
      {error}
    </p>
  ) : (
    <p className="page-loading">Opening chat…</p>
  )
}
