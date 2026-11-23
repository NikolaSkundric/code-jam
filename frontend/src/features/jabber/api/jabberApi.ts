import { getJson } from '@/lib/http'
import type { JabberInfo, XmppDashboard } from '../types'

export const jabberApi = {
  /** Admin only: connections and federation traffic of the Jabber servers. */
  dashboard: () => getJson<XmppDashboard>('/api/admin/jabber'),
  info: () => getJson<JabberInfo>('/api/jabber/info'),
}
