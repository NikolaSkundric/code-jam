import type { ReactNode } from 'react'
import ChangePasswordForm from '../components/ChangePasswordForm'
import DeleteAccountSection from '../components/DeleteAccountSection'
import { useAuth } from '../useAuth'
import './settings.css'

/** children: extra cards from other features, shown between the password form and account deletion. */
export default function SettingsPage({ children }: { children?: ReactNode }) {
  const { user } = useAuth()

  return (
    <section className="settings-page">
      <h1>Settings</h1>
      <p className="settings-hint">
        Signed in as <strong>{user?.username}</strong> ({user?.email})
      </p>

      <ChangePasswordForm />
      {children}
      <DeleteAccountSection />
    </section>
  )
}
