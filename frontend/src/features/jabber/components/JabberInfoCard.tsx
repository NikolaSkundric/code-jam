import { useEffect, useState } from 'react'
import { jabberApi } from '../api/jabberApi'
import type { JabberInfo } from '../types'
import { useAuth } from '@/features/auth'

/** Settings page card: how to sign in with any Jabber (XMPP) client. */
export default function JabberInfoCard() {
  const { user } = useAuth()
  const [info, setInfo] = useState<JabberInfo>()

  useEffect(() => {
    let cancelled = false
    jabberApi
      .info()
      .then((result) => {
        if (!cancelled) setInfo(result)
      })
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [])

  if (!info || !user) return null

  return (
    <section className="settings-card" aria-labelledby="jabber-title">
      <h2 id="jabber-title">Jabber (XMPP)</h2>
      <p className="settings-hint">
        You can also chat with any Jabber client (Gajim, Conversations, Pidgin, ...). Sign in with your website
        password. Your friends appear in the client as contacts, and chat messages between friends show up in both
        places, here and in the Jabber client.
      </p>
      <dl className="jabber-info">
        <dt>Jabber ID</dt>
        <dd>
          <code>
            {user.username.toLowerCase()}@{info.domain}
          </code>
        </dd>
        <dt>Server / port</dt>
        <dd>
          <code>localhost</code> port <code>{info.port}</code> (STARTTLS; accept the self-signed certificate)
        </dd>
        <dt>Password</dt>
        <dd>The same password as on this website</dd>
        <dt>Certificate</dt>
        <dd>
          Issued by the demo CA of this installation: import it into the client (or accept the warning). Get it with{' '}
          <code>docker cp chat-xmpp-a:/opt/chat-certs/ca.pem .</code>
        </dd>
      </dl>
    </section>
  )
}
