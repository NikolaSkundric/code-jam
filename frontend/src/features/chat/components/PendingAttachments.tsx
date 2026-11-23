import { formatBytes } from '@/lib/format'
import type { PendingAttachment } from '../types'

interface PendingAttachmentsProps {
  items: PendingAttachment[]
  onCommentChange: (id: string, comment: string) => void
  onRemove: (id: string) => void
}

/** Files chosen but not sent yet: preview, name, size, optional comment. */
export default function PendingAttachments({ items, onCommentChange, onRemove }: PendingAttachmentsProps) {
  if (items.length === 0) return null

  return (
    <ul className="pending-list">
      {items.map((item) => (
        <li key={item.id} className="pending-item">
          {item.previewUrl ? (
            <img className="pending-preview" src={item.previewUrl} alt="" />
          ) : (
            <span className="pending-icon" aria-hidden="true">
              📄
            </span>
          )}
          <div className="pending-body">
            <p className="pending-name">
              {item.file.name} <span className="muted">({formatBytes(item.file.size)})</span>
            </p>
            <input
              type="text"
              className="pending-comment"
              placeholder="Add a comment (optional)"
              maxLength={1000}
              value={item.comment}
              onChange={(e) => onCommentChange(item.id, e.target.value)}
              aria-label={`Comment for ${item.file.name}`}
            />
          </div>
          <button
            type="button"
            className="link-button"
            onClick={() => onRemove(item.id)}
            aria-label={`Remove ${item.file.name}`}
          >
            ×
          </button>
        </li>
      ))}
    </ul>
  )
}
