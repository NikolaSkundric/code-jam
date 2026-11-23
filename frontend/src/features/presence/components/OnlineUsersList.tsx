import PresenceDot from './PresenceDot'
import { usePresence } from '../usePresence'

/** Everyone currently online or AFK. */
export default function OnlineUsersList() {
  const { active, connected } = usePresence()

  if (!connected) return <p className="page-loading">Connecting…</p>
  if (active.length === 0) return <p>Nobody is online.</p>

  return (
    <ul className="online-list">
      {active.map((entry) => (
        <li key={entry.userId}>
          <PresenceDot status={entry.status} />
          <span>{entry.username}</span>
          <span className="online-status">{entry.status === 'afk' ? 'AFK' : 'online'}</span>
        </li>
      ))}
    </ul>
  )
}
