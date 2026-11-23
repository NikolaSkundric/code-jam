import { useState } from 'react'
import Modal from '@/components/ui/Modal'
import type { Message } from '../types'
import MessageAttachments from './MessageAttachments'

interface MessageItemProps {
  message: Message
  mine: boolean
  canWrite: boolean
  /** Room owner/admins can delete other people's messages. */
  canModerate?: boolean
  onReply: (message: Message) => void
  onEdit: (message: Message, text: string) => Promise<void>
  onDelete: (message: Message) => Promise<void>
  onMediaLoad: () => void
}

const timeFormat = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' })
const fullFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'medium' })

export default function MessageItem({
  message,
  mine,
  canWrite,
  canModerate = false,
  onReply,
  onEdit,
  onDelete,
  onMediaLoad,
}: MessageItemProps) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(message.text)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [busy, setBusy] = useState(false)

  const created = new Date(message.createdAt)
  const hasAttachments = message.attachments.length > 0

  async function saveEdit() {
    // The text may be emptied only when the message has attachments.
    if ((!draft.trim() && !hasAttachments) || draft === message.text) {
      setEditing(false)
      return
    }
    setBusy(true)
    try {
      await onEdit(message, draft)
      setEditing(false)
    } finally {
      setBusy(false)
    }
  }

  async function confirmDelete() {
    setBusy(true)
    try {
      await onDelete(message)
    } finally {
      setBusy(false)
      setConfirmingDelete(false)
    }
  }

  return (
    <article className={`message${mine ? ' message-mine' : ''}`}>
      <div className="message-head">
        <span className="message-time" title={fullFormat.format(created)}>
          [{timeFormat.format(created)}]
        </span>
        <span className="message-author">{mine ? 'You' : message.authorUsername}</span>
      </div>

      {message.replyTo && (
        <blockquote className="message-reply">
          <strong>{message.replyTo.authorUsername}</strong>
          {message.replyTo.deleted ? (
            <em> Original message was deleted</em>
          ) : (
            <span> {message.replyTo.text || '📎 Attachment'}</span>
          )}
        </blockquote>
      )}

      {editing ? (
        <div className="message-edit">
          <textarea
            value={draft}
            rows={2}
            maxLength={6000}
            onChange={(e) => setDraft(e.target.value)}
            aria-label="Edit message"
            autoFocus
          />
          <div className="message-actions-inline">
            <button type="button" className="btn-primary" disabled={busy} onClick={saveEdit}>
              Save
            </button>
            <button
              type="button"
              className="btn-secondary"
              disabled={busy}
              onClick={() => {
                setDraft(message.text)
                setEditing(false)
              }}
            >
              Cancel
            </button>
          </div>
        </div>
      ) : (
        <>
          {(message.text || message.editedAt) && (
            <p className="message-text">
              {message.text}
              {message.editedAt && (
                <span className="message-edited" title={fullFormat.format(new Date(message.editedAt))}>
                  {' '}
                  (edited)
                </span>
              )}
            </p>
          )}
          <MessageAttachments attachments={message.attachments} onMediaLoad={onMediaLoad} />
        </>
      )}

      {canWrite && !editing && (
        <div className="message-actions">
          <button type="button" className="link-button" onClick={() => onReply(message)}>
            Reply
          </button>
          {mine && (
            <button type="button" className="link-button" onClick={() => setEditing(true)}>
              Edit
            </button>
          )}
          {(mine || canModerate) && (
            <button type="button" className="link-button" onClick={() => setConfirmingDelete(true)}>
              Delete
            </button>
          )}
        </div>
      )}

      {confirmingDelete && (
        <Modal title="Delete message" onClose={() => setConfirmingDelete(false)}>
          <p className="delete-preview">
            {mine ? 'Delete your message?' : `Delete the message from ${message.authorUsername}?`}
          </p>
          <blockquote className="message-reply">
            {message.text || (hasAttachments ? '📎 Attachment' : '')}
          </blockquote>
          <div className="settings-actions">
            <button type="button" className="btn-danger" disabled={busy} onClick={confirmDelete}>
              {busy ? 'Deleting…' : 'Delete message'}
            </button>
            <button type="button" className="btn-secondary" disabled={busy} onClick={() => setConfirmingDelete(false)}>
              Cancel
            </button>
          </div>
        </Modal>
      )}
    </article>
  )
}
