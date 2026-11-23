import { useState } from 'react'
import type { FormEvent } from 'react'
import Modal from '@/components/ui/Modal'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../api/roomsApi'
import type { RoomVisibility } from '../types'

interface CreateRoomModalProps {
  onClose: () => void
  onCreated: (roomId: string) => void
}

export default function CreateRoomModal({ onClose, onCreated }: CreateRoomModalProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [visibility, setVisibility] = useState<RoomVisibility>('public')
  const [nameError, setNameError] = useState<string>()
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError('')
    if (name.trim().length < 3) {
      setNameError('Room name must be at least 3 characters')
      return
    }
    setNameError(undefined)

    setSubmitting(true)
    try {
      const room = await roomsApi.create({ name: name.trim(), description: description.trim(), visibility })
      onCreated(room.id)
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors.name) setNameError(err.fieldErrors.name)
      else setFormError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
      setSubmitting(false)
    }
  }

  return (
    <Modal title="Create room" onClose={onClose}>
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        {formError && (
          <p className="form-error" role="alert">
            {formError}
          </p>
        )}
        <TextField
          label="Room name"
          type="text"
          name="name"
          autoComplete="off"
          maxLength={100}
          value={name}
          onChange={(e) => setName(e.target.value)}
          error={nameError}
          autoFocus
        />
        <div className="field">
          <label htmlFor="room-description">Description</label>
          <textarea
            id="room-description"
            rows={3}
            maxLength={1000}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>
        <fieldset className="visibility-choice">
          <legend>Visibility</legend>
          <label className="checkbox">
            <input type="radio" name="visibility" checked={visibility === 'public'} onChange={() => setVisibility('public')} />
            Public - listed in the catalog, anyone can join
          </label>
          <label className="checkbox">
            <input type="radio" name="visibility" checked={visibility === 'private'} onChange={() => setVisibility('private')} />
            Private - invitation only
          </label>
        </fieldset>
        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Creating…' : 'Create room'}
        </button>
      </form>
    </Modal>
  )
}
