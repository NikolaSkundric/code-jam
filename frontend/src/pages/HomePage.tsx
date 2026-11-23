import { useAuth } from '@/features/auth'
import { OnlineUsersList } from '@/features/presence'
import './home.css'

/** Placeholder landing page after sign in; replaced by the chat layout later. */
export default function HomePage() {
  const { user } = useAuth()
  return (
    <section className="home-page">
      <h1>Welcome, {user?.username}</h1>
      <p>The chat view will live here.</p>

      <h2>People online</h2>
      <OnlineUsersList />
    </section>
  )
}
