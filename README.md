# Online Chat Server

A classic web chat: registration and login, public and private rooms, personal chats between friends, file and image sharing, online/AFK presence, moderation, and Jabber (XMPP) support with federation. The requirements are in [TASK.md](TASK.md).

**Stack:** React + Vite + TypeScript (nginx) · ASP.NET Core 8 Web API with SignalR · PostgreSQL · ejabberd (Jabber) · Docker Compose.

## Run it

You only need Docker with Compose.

```powershell
docker compose up --build
```

| What | Where |
|---|---|
| The chat | http://localhost:3000 |
| Mail inbox (password reset emails) | http://localhost:8025 |
| API (also behind the chat at `/api`) | http://localhost:5125 |
| Jabber client login | `localhost`, port `5222` |

Register an account on the site and you are in. Optionally copy `.env.example` to `.env` and set your own secrets; without it everything still starts with throwaway defaults for local use.

The user named `admin` (set `ADMIN_USERNAME` in `.env` to change it) also gets the administration page with the Jabber connection dashboard and federation traffic, under **Profile → Jabber admin**.

## Jabber (XMPP)

Any Jabber client can sign in as `<username>@a.chat.test` with the website password (connect to `localhost:5222`, STARTTLS). Friends appear as contacts, and chat messages and online status are shared with the website. A second server, `b.chat.test`, runs next to it to demonstrate federation over verified TLS. Export the demo certificate authority with:

```powershell
docker cp chat-xmpp-a:/opt/chat-certs/ca.pem .
```

Tests (run inside the compose network):

```powershell
docker compose --profile loadtest run --rm loadtest                    # 50 clients per server, 1000 messages across the federation
docker compose --profile loadtest run --rm loadtest node bridge-test.mjs   # Jabber <-> website bridge checks
```

## Load test of the web chat

```powershell
docker compose --profile loadtest run --rm webloadtest   # 300 simultaneous users, message and presence latency, a room with 10,000 messages
```

## More

- [CLAUDE.md](CLAUDE.md): architecture, folder structure, API reference, configuration and how each requirement is implemented.
- Code: `backend/CodingJamAPI` (API), `frontend` (web app), `xmpp` (Jabber servers), `tools/xmpp-loadtest` (tests).
