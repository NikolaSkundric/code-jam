import type { PresenceStatus } from '../types'
import './presence.css'

const LABELS: Record<PresenceStatus, string> = {
  online: 'Online',
  afk: 'AFK',
  offline: 'Offline',
}

interface PresenceDotProps {
  status: PresenceStatus
  /** Show the text label next to the dot. */
  showLabel?: boolean
}

/** Status indicator matching the wireframes: filled = online, half = AFK, hollow = offline. */
export default function PresenceDot({ status, showLabel = false }: PresenceDotProps) {
  return (
    <span className="presence" title={LABELS[status]}>
      <span className={`presence-dot presence-${status}`} aria-hidden="true" />
      {showLabel ? <span className="presence-label">{LABELS[status]}</span> : <span className="sr-only">{LABELS[status]}</span>}
    </span>
  )
}
