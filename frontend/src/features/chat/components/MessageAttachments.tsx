import { formatBytes } from '@/lib/format'
import { attachmentUrl } from '../types'
import type { Attachment } from '../types'

interface MessageAttachmentsProps {
  attachments: Attachment[]
  /** Called when an image finished loading (its height changed the layout). */
  onMediaLoad: () => void
}

/** Images inline (click opens the full image), other files as download links; both with the optional comment. */
export default function MessageAttachments({ attachments, onMediaLoad }: MessageAttachmentsProps) {
  if (attachments.length === 0) return null

  return (
    <div className="attachments">
      {attachments.map((attachment) => (
        <figure key={attachment.id} className="attachment">
          {attachment.isImage ? (
            <a href={attachmentUrl(attachment.id)} target="_blank" rel="noopener noreferrer">
              <img
                className="attachment-image"
                src={attachmentUrl(attachment.id)}
                alt={attachment.fileName}
                loading="lazy"
                onLoad={onMediaLoad}
              />
            </a>
          ) : (
            <a className="attachment-file" href={attachmentUrl(attachment.id)} download={attachment.fileName}>
              <span aria-hidden="true">📄</span>
              <span className="attachment-name">{attachment.fileName}</span>
              <span className="attachment-size">{formatBytes(attachment.sizeBytes)}</span>
            </a>
          )}
          {attachment.comment && <figcaption className="attachment-comment">{attachment.comment}</figcaption>}
        </figure>
      ))}
    </div>
  )
}
