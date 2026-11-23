export interface XmppSession {
  jid: string
  connection: string
  ip: string
  port: number
  priority: number
  uptimeSeconds: number
  status: string
}

/** Stanzas exchanged with one remote server since the Jabber server started. */
export interface XmppFederationTraffic {
  peer: string
  messagesOut: number
  messagesIn: number
  presenceOut: number
  presenceIn: number
  iqOut: number
  iqIn: number
}

export interface XmppServer {
  name: string
  domain: string
  available: boolean
  error: string | null
  version: string | null
  uptimeSeconds: number
  connectedUsers: number
  incomingServerConnections: number
  outgoingServerConnections: number
  sessions: XmppSession[]
  traffic: XmppFederationTraffic[]
}

export interface XmppDashboard {
  local: XmppServer
  peer: XmppServer | null
  generatedAt: string
}

export interface JabberInfo {
  domain: string
  port: number
}
