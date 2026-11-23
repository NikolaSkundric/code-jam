import ChangePasswordForm from '../components/ChangePasswordForm'
import DeleteAccountSection from '../components/DeleteAccountSection'
import { useAuth } from '../useAuth'
import './settings.css'

export default function SettingsPage() {
  const { user } = useAuth()

  return (
    <section className="settings-page">
      <h1>Settings</h1>
      <p className="settings-hint">
        Signed in as <strong>{user?.username}</strong> ({user?.email})
      </p>

      <ChangePasswordForm />
      <DeleteAccountSection />
    </section>
  )
}
