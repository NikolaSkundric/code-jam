import { useState } from 'react'
import Modal from '@/components/ui/Modal'
import { useAuth } from '@/features/auth'
import type { RoomDetails } from '../types'
import AdminsTab from './manage/AdminsTab'
import BansTab from './manage/BansTab'
import InvitationsTab from './manage/InvitationsTab'
import MembersTab from './manage/MembersTab'
import SettingsTab from './manage/SettingsTab'

type Tab = 'members' | 'admins' | 'bans' | 'invitations' | 'settings'

interface ManageRoomModalProps {
  room: RoomDetails
  onChanged: () => Promise<void>
  onDeleted: () => void
  onClose: () => void
}

/** Admin actions as a modal with tabs (TASK.md 4.5, wireframe "Manage Room"). */
export default function ManageRoomModal({ room, onChanged, onDeleted, onClose }: ManageRoomModalProps) {
  const { user } = useAuth()
  const [tab, setTab] = useState<Tab>('members')

  const tabs: { id: Tab; label: string }[] = [
    { id: 'members', label: 'Members' },
    { id: 'admins', label: 'Admins' },
    { id: 'bans', label: 'Banned users' },
    { id: 'invitations', label: 'Invitations' },
    ...(room.myRole === 'owner' ? [{ id: 'settings' as const, label: 'Settings' }] : []),
  ]

  return (
    <Modal title={`Manage room: #${room.name}`} onClose={onClose} wide>
      <div className="tabs" role="tablist">
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            className={`tab${tab === t.id ? ' tab-active' : ''}`}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="tab-panel" role="tabpanel">
        {tab === 'members' && <MembersTab room={room} currentUserId={user?.id ?? ''} onChanged={onChanged} />}
        {tab === 'admins' && <AdminsTab room={room} onChanged={onChanged} />}
        {tab === 'bans' && <BansTab roomId={room.id} />}
        {tab === 'invitations' && <InvitationsTab roomId={room.id} />}
        {tab === 'settings' && <SettingsTab room={room} onChanged={onChanged} onDeleted={onDeleted} />}
      </div>
    </Modal>
  )
}
