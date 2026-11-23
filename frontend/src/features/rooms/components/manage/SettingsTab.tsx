import { useState } from 'react'
import type { FormEvent } from 'react'
import TextField from '@/components/ui/TextField'
import { ApiError } from '@/lib/http'
import { roomsApi } from '../../api/roomsApi'
import type { RoomDetails, RoomVisibility } from '../../types'

interface SettingsTabProps {
  room: RoomDetails
  onChanged: () => Promise<void>
  onDeleted: () => void
}

/** Owner-only: edit name, description and visibility; delete the room. */
export default function SettingsTab({ room, onChanged, onDeleted }: SettingsTabProps) {
  const [name, setName] = useState(room.name)
  const [description, setDescription] = useState(room.description)
  const [visibility, setVisibility] = useState<RoomVisibility>(room.visibility)
  const [nameError, setNameError] = useState<string>()
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)
  const [saving, setSaving] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [deleting, setDeleting] = useState(false)

  async function save(event: FormEvent) {
    event.preventDefault()
    setSaved(false)
    setError('')
    if (name.trim().length < 3) {
      setNameError('Room name must be at least 3 characters')
      return
    }
    setNameError(undefined)

    setSaving(true)
    try {
      await roomsApi.update(room.id, { name: name.trim(), description: description.trim(), visibility })
      setSaved(true)
      await onChanged()
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors.name) setNameError(err.fieldErrors.name)
      else setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  async function deleteRoom() {
    setDeleting(true)
    setError('')
    try {
      await roomsApi.remove(room.id)
      onDeleted()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not delete the room.')
      setDeleting(false)
    }
  }

  return (
    <div>
      <form className="auth-form" onSubmit={save} noValidate>
        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        {saved && (
          <p className="form-success" role="status">
            Changes saved.
          </p>
        )}
        <TextField
          label="Room name"
          type="text"
          name="roomName"
          autoComplete="off"
          maxLength={100}
          value={name}
          onChange={(e) => setName(e.target.value)}
          error={nameError}
        />
        <div className="field">
          <label htmlFor="settings-description">Description</label>
          <textarea
            id="settings-description"
            rows={3}
            maxLength={1000}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>
        <fieldset className="visibility-choice">
          <legend>Visibility</legend>
          <label className="checkbox">
            <input type="radio" name="settings-visibility" checked={visibility === 'public'} onChange={() => setVisibility('public')} />
            Public
          </label>
          <label className="checkbox">
            <input type="radio" name="settings-visibility" checked={visibility === 'private'} onChange={() => setVisibility('private')} />
            Private
          </label>
        </fieldset>
        <button type="submit" className="btn-primary" disabled={saving}>
          {saving ? 'Saving…' : 'Save changes'}
        </button>
      </form>

      <section className="settings-card settings-danger manage-danger">
        <h3>Delete room</h3>
        <p className="settings-hint">
          All messages, files and images in this room are deleted permanently. This cannot be undone.
        </p>
        {confirmingDelete ? (
          <div className="settings-actions">
            <button type="button" className="btn-danger" disabled={deleting} onClick={deleteRoom}>
              {deleting ? 'Deleting…' : 'Permanently delete this room'}
            </button>
            <button type="button" className="btn-secondary" disabled={deleting} onClick={() => setConfirmingDelete(false)}>
              Cancel
            </button>
          </div>
        ) : (
          <button type="button" className="btn-danger" onClick={() => setConfirmingDelete(true)}>
            Delete room…
          </button>
        )}
      </section>
    </div>
  )
}
