import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import type { ChangeEvent, ClipboardEvent, FormEvent, KeyboardEvent } from 'react'
import { formatBytes } from '@/lib/format'
import {
  IMAGE_TYPES,
  MAX_FILE_BYTES,
  MAX_FILES_PER_MESSAGE,
  MAX_IMAGE_BYTES,
  MAX_MESSAGE_BYTES,
} from '../types'
import type { Message, PendingAttachment } from '../types'
import EmojiPicker from './EmojiPicker'
import PendingAttachments from './PendingAttachments'

interface MessageComposerProps {
  disabled: boolean
  replyTo: Message | null
  onCancelReply: () => void
  onSend: (text: string, attachments: PendingAttachment[]) => Promise<void>
}

const encoder = new TextEncoder()

function revoke(items: PendingAttachment[]) {
  for (const item of items) if (item.previewUrl) URL.revokeObjectURL(item.previewUrl)
}

export default function MessageComposer({ disabled, replyTo, onCancelReply, onSend }: MessageComposerProps) {
  const [text, setText] = useState('')
  const [pending, setPending] = useState<PendingAttachment[]>([])
  const [showEmoji, setShowEmoji] = useState(false)
  const [error, setError] = useState('')
  const [sending, setSending] = useState(false)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const pendingCursor = useRef<number | null>(null)
  const pendingRef = useRef(pending)

  useEffect(() => {
    pendingRef.current = pending
  }, [pending])

  // Free the preview object URLs when the composer goes away.
  useEffect(() => () => revoke(pendingRef.current), [])

  // After an emoji is inserted, put the caret right behind it.
  useLayoutEffect(() => {
    if (pendingCursor.current === null) return
    inputRef.current?.setSelectionRange(pendingCursor.current, pendingCursor.current)
    pendingCursor.current = null
  }, [text])

  const canSend = !disabled && !sending && (text.trim().length > 0 || pending.length > 0)

  function addFiles(files: File[]) {
    setError('')
    const accepted: PendingAttachment[] = []
    let room = MAX_FILES_PER_MESSAGE - pending.length

    for (const file of files) {
      const isImage = IMAGE_TYPES.includes(file.type)
      if (room <= 0) {
        setError(`You can attach at most ${MAX_FILES_PER_MESSAGE} files to a message.`)
        break
      }
      if (file.size === 0) {
        setError(`${file.name} is empty.`)
        continue
      }
      if (isImage && file.size > MAX_IMAGE_BYTES) {
        setError(`${file.name} is too large (${formatBytes(file.size)}). Images can be up to 3 MB.`)
        continue
      }
      if (file.size > MAX_FILE_BYTES) {
        setError(`${file.name} is too large (${formatBytes(file.size)}). Files can be up to 20 MB.`)
        continue
      }
      accepted.push({
        id: crypto.randomUUID(),
        file,
        comment: '',
        previewUrl: isImage ? URL.createObjectURL(file) : undefined,
      })
      room--
    }
    if (accepted.length > 0) setPending((current) => [...current, ...accepted])
  }

  function onPickFiles(event: ChangeEvent<HTMLInputElement>) {
    addFiles(Array.from(event.target.files ?? []))
    event.target.value = '' // allow picking the same file again
  }

  // Paste (e.g. a screenshot) attaches the files from the clipboard instead of inserting text.
  function onPaste(event: ClipboardEvent<HTMLTextAreaElement>) {
    const files = Array.from(event.clipboardData.files)
    if (files.length === 0 || disabled) return
    event.preventDefault()
    addFiles(files)
  }

  function removePending(id: string) {
    setPending((current) => {
      revoke(current.filter((p) => p.id === id))
      return current.filter((p) => p.id !== id)
    })
  }

  async function submit(event?: FormEvent) {
    event?.preventDefault()
    if (!canSend) return

    if (encoder.encode(text).length > MAX_MESSAGE_BYTES) {
      setError('Message is too long (maximum 3 KB).')
      return
    }

    setSending(true)
    setError('')
    try {
      await onSend(text, pending)
      revoke(pending)
      setPending([])
      setText('')
      setShowEmoji(false)
      inputRef.current?.focus()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not send the message.')
    } finally {
      setSending(false)
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    // Enter sends, Shift+Enter inserts a new line.
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault()
      void submit()
    }
  }

  function insertEmoji(emoji: string) {
    const el = inputRef.current
    const start = el?.selectionStart ?? text.length
    const end = el?.selectionEnd ?? text.length
    const next = text.slice(0, start) + emoji + text.slice(end)
    pendingCursor.current = start + emoji.length
    setText(next)
    el?.focus() // focus right away so typing continues in the message box
  }

  return (
    <form className="composer" onSubmit={submit}>
      {replyTo && (
        <div className="composer-reply">
          <span>
            Replying to: <strong>{replyTo.authorUsername}</strong>{' '}
            <span className="muted">{replyTo.text || '📎 Attachment'}</span>
          </span>
          <button type="button" className="link-button" onClick={onCancelReply} aria-label="Cancel reply">
            ×
          </button>
        </div>
      )}
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      <PendingAttachments
        items={pending}
        onCommentChange={(id, comment) =>
          setPending((current) => current.map((p) => (p.id === id ? { ...p, comment } : p)))
        }
        onRemove={removePending}
      />
      {showEmoji && <EmojiPicker onPick={insertEmoji} />}
      <div className="composer-row">
        <button
          type="button"
          className="btn-secondary composer-emoji"
          onClick={() => setShowEmoji((v) => !v)}
          disabled={disabled}
          aria-label="Emoji"
          aria-expanded={showEmoji}
        >
          😊
        </button>
        <button
          type="button"
          className="btn-secondary composer-emoji"
          onClick={() => fileInputRef.current?.click()}
          disabled={disabled}
          aria-label="Attach files"
        >
          📎
        </button>
        <input ref={fileInputRef} type="file" multiple hidden onChange={onPickFiles} />
        <textarea
          ref={inputRef}
          value={text}
          rows={2}
          maxLength={6000}
          placeholder={disabled ? 'This chat is read-only' : 'Write a message… (Shift+Enter for a new line)'}
          disabled={disabled}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={onKeyDown}
          onPaste={onPaste}
          aria-label="Message"
        />
        <button type="submit" className="btn-primary" disabled={!canSend}>
          {sending ? 'Sending…' : 'Send'}
        </button>
      </div>
    </form>
  )
}
