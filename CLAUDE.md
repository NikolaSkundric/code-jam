# Online Chat Server

Classic web chat application. Full requirements: see [TASK.md](TASK.md) (source of truth for behavior).

## 1. Tech Stack

### 1.1 Frontend
- React 18 + Vite + TypeScript
- Real-time via `@microsoft/signalr` client
- Served in production by nginx (static build, reverse-proxies `/api` and `/hubs` to the backend)

### 1.2 Backend
- ASP.NET Core 8 (LTS) Web API, C#
- Real-time messaging, presence and unread notifications over SignalR hubs
- Custom authentication (no ASP.NET Core Identity): JWT Bearer tokens delivered in an `HttpOnly`, `Secure`, `SameSite` cookie, so the token is never readable from JavaScript; the JWT Bearer handler reads the token from that cookie (and SignalR gets it the same way)
- Short-lived access JWT with a unique `jti`; "Keep me signed in" sets a persistent cookie / longer lifetime
- Server-side `sessions` table (one row per browser: `jti`, user, user agent, IP, created/last seen, expiry, revoked flag); every request validates the token's `jti` against it, so sign out and "log out selected session" revoke only that session
- CSRF protection for state-changing requests (SameSite plus anti-forgery header check)
- Passwords hashed with BCrypt (`BCrypt.Net-Next`); password reset via single-use, expiring tokens
- Attachments stored on the local file system (Docker volume), served only through authorized API endpoints; limits: 20 MB files, 3 MB images

### 1.3 Database
- PostgreSQL
- EF Core with the Npgsql provider; code-first migrations applied on startup

### 1.4 Infrastructure
- Docker Compose in the repo root: `frontend` (nginx), `api`, `postgres`, plus a volume for uploaded files and one for database data
- Everything must build and run with `docker compose up`
- Repo layout: `backend/`, `frontend/`

## 2. Wireframes

```text
+----------------------------------------------------------------------------------+
| LOGO                                                     [Sign in] [Register]    |
+----------------------------------------------------------------------------------+
+----------------------------------+    +----------------------------------------+
|            SIGN IN               |    |               REGISTER                 |
|----------------------------------|    |----------------------------------------|
| Email                            |    | Email                                  |
| [______________________________] |    | [______________________________]       |
| Password                         |    | Username                               |
| [______________________________] |    | [______________________________]       |
| [ ] Keep me signed in            |    | Password                               |
|                                  |    | [______________________________]       |
| ( Sign in )                      |    | Confirm password                       |
|                                  |    | [______________________________]       |
| Forgot password?                 |    |                                        |
+----------------------------------+    | ( Create account )                     |
                                        +----------------------------------------+
+-------------------------------------------------------------+
| Forgot password                                              |
|-------------------------------------------------------------|
| Enter your email to reset password                          |
| [______________________________________________]            |
| ( Send reset link )                                         |
+-------------------------------------------------------------+

+------------------------------------------------------------------------------------------------------+
| ChatLogo | Public Rooms | Private Rooms | Contacts | Sessions | Profile ▼ | Sign out                |
+------------------------------------------------------------------------------------------------------+
+----------------------------+------------------------------------------------+-------------------------+
| RIGHT SIDEBAR (collapsable)|                 MAIN CHAT                      | MEMBERS / CONTEXT       |
| (rooms + contacts)         |                                                |                         |
|----------------------------|------------------------------------------------|-------------------------|
| Search [______________]    | # engineering-room                             | Room info               |
|                            | "Classic room description here..."             | Public room             |
| ROOMS                      |------------------------------------------------| Owner: alice            |
| > Public Rooms             | [10:21] Bob: Hello team                        | Admins                  |
|   • general        (3)     | [10:22] Alice: Uploading spec                  | - alice                 |
|   • engineering           | [10:23] You: Here's the file                    | - dave                  |
|   • random                |     ┌───────────────────────────────┐           |                         |
|                            |    | spec-v3.pdf                  |            | Members (38)            |
| > Private Rooms            |    | comment: latest requirements |            | ● Alice                 |
|   • core-team      (1)     |    └───────────────────────────────┘           | ● Bob                   |
|   • ops                    |                                                | ◐ Carol (AFK)           |
|                            | [10:25] Carol replied to Bob:                  | ○ Mike (offline)        |
| CONTACTS                   |   > Hello team                                 |                         |
|   ● Alice                  |   Can we make this private?                    | [Invite user]           |
|   ◐ Bob                    |                                                | [Manage room]          |
|   ○ Carol          (2)     | ---------------- older messages ----------------|                        |
|                            |                                                 |                        |
| [Create room]              |                                                 |                        |
+----------------------------+------------------------------------------------+-------------------------+
| [😊] [Attach] [Replying to: Bob ×]                                             [ message input      ] |
|                                                                                [ multiline text...  ] |
|                                                                                [ Send ]               |
+-------------------------------------------------------------------------------------------------------+

+----------------------------------------------------------------------------------+
| Manage Room: #engineering-room                                              [X]  |
+----------------------------------------------------------------------------------+
| Tabs: [Members] [Admins] [Banned users] [Invitations] [Settings]                 |
+----------------------------------------------------------------------------------+

[Members]
+----------------------------------------------------------------------------------+
| Search member [____________________________]                                      |
|----------------------------------------------------------------------------------|
| Username        Status      Role        Actions                                   |
| alice           online      Owner       --                                        |
| dave            AFK         Admin       [Remove admin] [Ban]                      |
| bob             online      Member      [Make admin] [Ban] [Remove from room]     |
| carol           offline     Member      [Make admin] [Ban] [Remove from room]     |
+----------------------------------------------------------------------------------+

[Admins]
+----------------------------------------------------------------------------------+
| Current admins: alice, dave                                                      |
| alice = owner (cannot lose admin rights)                                         |
| dave  [Remove admin]                                                              |
+----------------------------------------------------------------------------------+

[Banned users]
+----------------------------------------------------------------------------------+
| Username        Banned by      Date/time              Actions                     |
| mike            alice          2026-04-18 13:25       [Unban]                     |
| eve             dave           2026-04-18 13:40       [Unban]                     |
+----------------------------------------------------------------------------------+

[Invitations]
+----------------------------------------------------------------------------------+
| Invite by username [____________________] [Send invite]                          |
+----------------------------------------------------------------------------------+

[Settings]
+----------------------------------------------------------------------------------+
| Room name        [ engineering-room ]                                            |
| Description      [ backend + frontend discussions ]                              |
| Visibility       (•) Public   ( ) Private                                        |
|                                                                                  |
| [ Save changes ]                                             [ Delete room ]     |
+----------------------------------------------------------------------------------+
```

## 3. Backend Folder Structure (`backend/CodingJamAPI`)

Single project, layered by folder (dependencies point inward: Api -> Application -> Domain; Infrastructure implements Application abstractions).

```text
Controllers/            HTTP endpoints, thin: validate, call a service, map result
Hubs/                   SignalR hubs (chat, presence)
Middleware/             Exception handling, session (jti) validation, CSRF check
Extensions/             IServiceCollection / IApplicationBuilder registration helpers
Options/                Strongly typed settings (Jwt, Storage, ...)
Contracts/
  Requests/             Incoming API models
  Responses/            Outgoing API models
Domain/
  Entities/             User, Room, Message, Attachment, Session, Friendship, ...
  Enums/                RoomVisibility, PresenceStatus, ...
  Exceptions/           Domain/business rule exceptions
Application/
  Abstractions/         Interfaces (repositories, IPasswordHasher, IFileStorage, ITokenService)
  Services/             Use-case logic (auth, rooms, messages, friends, ...)
  Validators/           Request validation
  Mappings/             Entity <-> contract mapping
  Models/               Internal result/value types (AuthResult, ClientInfo)
Infrastructure/
  Persistence/          DbContext
    Configurations/     EF Core entity configurations
    Migrations/         EF Core migrations
    Repositories/       Repository implementations
  Auth/                 JWT issuing, BCrypt hasher, session store
  Storage/              Local file system attachment storage
  Realtime/             Presence tracking, SignalR notifiers
```

## 4. Frontend Theme

Exactly three main theme colors, defined once in `frontend/src/styles/theme.css` (CSS variables; mirrored in `theme.ts`). Always use the variables, never hardcode colors. All other tokens (text, borders, hover, error) are derived from these three.

| Token | Color | Role |
|---|---|---|
| `--color-surface` | `#f5f5f5` (whitesmoke) | Page background, surfaces |
| `--color-primary` | `#c2512f` (terracotta) | Buttons, links, top bar, focus, brand |
| `--color-accent` | `#f2b134` (amber) | Highlights, active nav, dividers |

### Frontend structure (`frontend/src`)

```text
main.tsx              Entry point
app/                  App shell: App.tsx (providers, BrowserRouter), AppRouter.tsx (routes)
features/<feature>/   Feature modules (auth, later rooms, messages, contacts, ...)
  api/                Requests for the feature
  components/         Feature-specific components and styles
  pages/              Route-level screens
  utils/              Feature helpers (validation, ...)
  index.ts            Public API of the feature (import features only through this)
components/ui/        Reusable, feature-agnostic UI (TextField, form.css)
pages/                Pages that do not belong to a feature (HomePage placeholder)
lib/                  Shared infrastructure (http client, ApiError)
styles/               index.css (global), theme.css + theme.ts (the 3 theme colors)
```

Conventions: use the `@/` alias for imports from `src` (relative imports only inside the same feature folder); cross-feature imports go through the feature `index.ts`; the JWT lives in an HttpOnly cookie, so fetch uses `credentials: include`. Routes: public `/login`, `/register`, `/forgot-password`, `/reset-password?token=`; protected (inside `RequireAuth` + `AppLayout`) `/`, `/sessions`, `/messages`, `/messages/:chatId`, `/messages/with/:userId` (personal chats), `/rooms` (public catalog), `/rooms/private` (my private rooms and invitations), `/rooms/:roomId`, `/contacts` (friends and friend requests), `/settings` (change password, delete account). `AuthProvider` restores the user from `GET /api/auth/me`; use `useAuth()`. Dev server proxies `/api` and `/hubs` to `localhost:5125`.

## 5. Auth API (`/api/auth`)

JWT (sub = user id, jti = session id) in an HttpOnly cookie `access_token`; every request is validated against the `UserSessions` row (revoked/expired/deleted user = 401). State-changing `/api` requests must send header `X-Requested-With` (CSRF defence; the frontend http client does).

| Endpoint | Auth | Purpose |
|---|---|---|
| `POST register` | no | Create account, sign in (201 + user) |
| `POST login` | no | Email + password, `keepSignedIn` = persistent cookie (30 days) else session cookie (24 h) |
| `POST logout` | yes | Revoke current session only |
| `GET me` | yes | Current user |
| `POST forgot-password` | no | Always 202 (same answer for unknown emails); the reset link is emailed in the background (see below) |
| `POST reset-password` | no | `{token, newPassword}`; single use, 1 h; signs out all sessions |
| `POST change-password` | yes | `{currentPassword, newPassword}`; revokes the other sessions |
| `GET sessions` / `DELETE sessions/{id}` | yes | List / revoke active sessions (browser, IP) |
| `DELETE account` | yes | `{password}`; soft deletes the user, permanently deletes owned rooms |

Sessions are sliding: once half of a session lifetime (24 h, or 30 days with "Keep me signed in", checked by default) has passed, the next request extends the session and re-issues the cookie, so active users are never logged out. The persistent choice travels in the JWT claim `rem`.

Email: `Email:*` settings. With `Email__Host` set (docker compose points it at the bundled Mailpit container) `SmtpEmailSender` sends the reset mail; the inbox is at http://localhost:8025. Without a host, `LoggingEmailSender` only logs the link. Failures never change the API response.

Errors are RFC 7807 problem details; field errors are in `errors` (camelCase keys for business errors).

## 6. Presence (online / AFK / offline)

SignalR hub `/hubs/presence` (HttpOnly-cookie auth), one connection per browser tab. Tabs call `Activity` on user interaction (mouse, keys, scroll, touch, focus; throttled to one report per 3 s). `PresenceTracker` (singleton) combines all tabs of a user: **online** if any tab had activity within `Presence:AfkAfterSeconds` (60), **AFK** if tabs are open but all idle, **offline** when no tab is open (connection closed). `PresenceSweeperService` re-evaluates every second and the hub pushes `PresenceChanged {userId, username, status}`; `GetSnapshot` returns everyone online/AFK. Frontend: `features/presence` (`PresenceProvider` in `AppLayout`, `usePresence()`, `PresenceDot`: filled = online, half = AFK, hollow = offline).

Presence is visible only to the user, their friends and people who share a room with them (TASK.md 2.2.1): `IPresenceAudienceService` computes that symmetric set; `PresenceChanged` is sent only to it (`Clients.Users`) and `GetSnapshot` returns only those people; the client re-reads the snapshot on `FriendsChanged` / `RoomsChanged`. Presence is held in memory (single API instance).

## 7. Friends (`/api/friends`)

Friend requests are sent by username with an optional message (max 500 chars) and must be accepted by the recipient. A friendship is stored once per pair (`GuidPair.Order`: ids compared as canonical text so the order matches PostgreSQL uuid ordering; never use `Guid.CompareTo` for pair ordering). Removing a friend, cancelling or accepting is a soft delete / status change; a friend can be re-added later.

| Endpoint | Purpose |
|---|---|
| `GET /api/friends` | Friends list |
| `DELETE /api/friends/{userId}` | Remove a friend (either side) |
| `GET /api/friends/requests` | Pending `{incoming, outgoing}` |
| `POST /api/friends/requests` | `{username, message?}`; rejects unknown user, self, already friends, pending in either direction, and banned users |
| `POST /api/friends/requests/{id}/accept` / `.../decline` | Recipient only |
| `DELETE /api/friends/requests/{id}` | Sender withdraws a pending request |

Every change pushes `FriendsChanged` over the existing SignalR connection (`Clients.Users`, user id = JWT `sub` via `SubUserIdProvider`) to both people, and the frontend `FriendsProvider` refetches. Not built yet: sending requests from a room member list (2.3.2), which needs rooms.

### User-to-user ban (`/api/bans`, TASK.md 2.3.5)

`GET /api/bans` (users I banned), `POST /api/bans {username}`, `DELETE /api/bans/{userId}` (unban, soft delete). Banning in one transaction: creates the `UserBan`, soft deletes the friendship and withdraws pending friend requests between the two; both users get `FriendsChanged`. A ban in either direction means the pair is **blocked** (`IBanService.IsBlockedAsync`): no friend requests (send or accept) and, for the future messaging code, no new personal messages. The personal chat history stays visible but is read-only, i.e. a personal chat is writable only if the two users are friends AND not blocked (TASK.md 2.3.6) - derive this at send time, nothing is stored on `PersonalChat`. Frontend: Ban on the friends list (with a confirmation) and on received requests, plus a Banned users section with Unban and ban-by-username on `/contacts`.

## 8. Personal chats and messages

A personal dialog is a `Chat` (type Personal) + `PersonalChat` (the two users, ordered with `GuidPair`); messages, edits, replies and read state work on any chat via `IMessageService`, so rooms reuse them. `IChatAccessService.GetAccessAsync(user, chat)` is the one place deciding access: null = not a participant (404), `CanWrite` = friends AND not blocked, otherwise the chat is frozen (history readable, send/edit/delete return 403 "read-only", TASK.md 2.3.5/2.3.6). New dialogs can only be opened with friends.

| Endpoint | Purpose |
|---|---|
| `GET /api/personal-chats` | Dialogs with last message, `unreadCount`, `canWrite` (incl. frozen ones) |
| `POST /api/personal-chats {userId}` | Open/create the dialog with a friend |
| `GET /api/chats/{id}/messages?limit=30&before={messageId}` | Newest page, or older than `before` (infinite scroll); oldest first, `hasMore` |
| `POST /api/chats/{id}/messages {text, replyToMessageId?}` | Send (text max 3 KB UTF-8, multiline/emoji ok, blank rejected) |
| `PUT` / `DELETE /api/chats/{id}/messages/{mid}` | Edit (sets `editedAt`) / soft delete, author only |
| `POST /api/chats/{id}/read` | Clear the unread indicator |

Pagination cursor is `(CreatedAt, Id)`. Unread = other users' messages newer than `ChatReadState.LastReadAt`. Replies show a preview; if the original is deleted the preview is marked deleted. Push events over the SignalR connection to both participants: `MessageReceived`, `MessageEdited`, `MessageDeleted`, `ConversationsChanged` (own tabs after read); `FriendsChanged` also refreshes the conversation list. Frontend: `features/chat` (`ChatProvider` = conversation list + unread total, `MessageThread` = infinite scroll up, autoscroll only when already at the bottom, composer with Enter=send / Shift+Enter=newline, emoji picker, reply bar). `PresenceProvider` is the app-wide SignalR connection; use `usePresence().subscribe(event, handler)` for push events.


### Attachments (TASK.md 2.6, 3.4)

Messages can carry up to 5 files (images and arbitrary files). `POST /api/chats/{id}/messages` accepts `multipart/form-data` (`text`, `replyToMessageId`, `files[]`, `comments[]` aligned by index) besides the JSON text-only form; the frontend always uses multipart. Text may be empty when files are attached. Files can be added with the paperclip button or by pasting from the clipboard; each can have an optional comment (max 1000 chars); the original file name is kept (paths and control characters stripped).

- **Limits** (`Storage` options): 20 MB per file, 3 MB per image, 5 files per message; nginx allows 110 MB bodies.
- **Images** are detected by magic bytes (png, jpeg, gif, webp), never by the client content type or extension. Everything else, including SVG, is a plain file. Oversize or empty files are rejected before anything is stored.
- **Storage:** `IFileStorage` / `LocalFileStorage` writes to `Storage:RootPath` (`/app/uploads`, the `uploads-data` Docker volume) under server-generated keys `chatId/attachmentId`; the user file name is only metadata. If the database save fails the written files are removed.
- **Access:** `GET /api/attachments/{id}` checks `IChatAccessService` (current participant of the chat; frozen chats can still be read). No access and unknown id both return 404. Verified images are served inline with a sandbox CSP; all other files are `application/octet-stream` downloads; `nosniff` always.
- **Deletion:** deleting a message soft deletes its attachments (404 afterwards); files stay on disk (soft delete policy). Deleting a room must also remove its `uploads/{chatId}` directory (TASK.md 2.4.6) - to do with rooms.

## 9. Chat rooms (`/api/rooms`, `/api/invitations`)

A room is a `Chat` (type Room) + `Room` (same id) + `RoomMember` rows (role `owner` / `admin` / `member`; the owner always has an active row). A room id is also its chat id, so messages, edits, attachments and unread tracking use the same `/api/chats/{roomId}/...` endpoints as personal chats; `IChatAccessService` gives room members `CanWrite` and room owner/admins `CanModerate` (delete anyone's message, TASK.md 2.4.7). A removed, banned or departed user is no longer a member, so they lose messages and files at once (2.4.8). Enums are serialized as camelCase strings.

| Endpoint | Purpose |
|---|---|
| `GET /api/rooms` / `POST /api/rooms` | My rooms (with unread, member count, my role) / create (name 3-100, unique case-insensitively, description, `public`/`private`) |
| `GET /api/rooms/catalog?search=` | Public rooms only: name, description, member count (ILIKE search on name and description), `isMember` |
| `GET /api/rooms/{id}` | Details + members (members only; others get 404) |
| `PUT` / `DELETE /api/rooms/{id}` | Owner only: edit settings / delete permanently |
| `POST .../join`, `POST .../leave` | Join a public room (403 when banned; private rooms return 404) / leave (the owner cannot; 400) |
| `PUT .../members/{userId}/role {isAdmin}` | Owner or admin; the owner can never lose admin |
| `GET`/`POST .../bans`, `DELETE .../bans/{userId}` | Admins: list (who banned, when) / remove a member and ban them / unban. Admins can act on members only, the owner also on admins; nobody on the owner |
| `GET`/`POST .../invitations`, `DELETE .../invitations/{id}` | Any member invites by username (not for banned or blocked users); admins list/cancel pending ones |
| `GET /api/invitations`, `POST .../{id}/accept\|decline` | The invited user's own invitations (private rooms are joined only this way) |

Deleting a room (TASK.md 2.4.6): `ExecuteDelete` on the `Chat` row cascades to room, members, bans, invitations, messages (including soft deleted) and attachment rows, then `IFileStorage.DeleteFolder` removes `uploads/{roomId}`. Max 1000 members per room. Push event `RoomsChanged` (payload room id) goes to members/affected users; the frontend `RoomsProvider` and `RoomPage` refetch. Frontend: `features/rooms` (sidebar section with public/private accordion + invitations, catalog page, `RoomPage` = thread + members panel, `ManageRoomModal` with Members/Admins/Banned users/Invitations/Settings tabs); `src/app/ChatLayout.tsx` composes the sidebar (rooms + contacts) around `/messages/*` and `/rooms/*`; `MessageThread` is shared by personal chats and rooms. Friend requests can be sent from the members panel ("Add friend", TASK.md 2.3.2).

### UI layout notes (TASK.md 4.1)

Top menu (wireframe): Chats, Public Rooms (catalog), Private Rooms, Contacts, Sessions, Profile menu (name, status, Settings), Sign out. The chat sidebar (left, as drawn in the wireframe) is collapsible (preference kept in localStorage) and its Public rooms / Private rooms / Contacts sections are an accordion: all open on the overview, and entering a room or chat compacts it to the group that contains it. Deleting an account also removes the upload folders of the rooms it owned. Deleting a message asks for confirmation in a modal dialog (TASK.md 4.5).

## 10. Configuration and secrets

No secret is stored in the repository. `.env` (git-ignored; template: `.env.example`) holds `POSTGRES_PASSWORD` and `JWT_SIGNING_KEY`. `docker-compose.yml` reads it automatically and passes the values to the containers (`${POSTGRES_PASSWORD:-postgres}`, `${JWT_SIGNING_KEY:-}`). Outside Docker (`dotnet run`, Visual Studio, `dotnet ef`) the API reads the same file in Development (`Extensions/DotEnv.cs`), building the connection string for the published postgres port.

- Fresh clone without `.env`: `docker compose up` still works. The database falls back to the throwaway password `postgres` (published on 127.0.0.1 only), and the API generates a random JWT key at startup, so users are signed out whenever the API restarts. Copy `.env.example` to `.env` and set real values for anything beyond a local demo.
- The postgres password is applied only when the data volume is first created. Change it later with `ALTER USER postgres PASSWORD '...'` (or `docker compose down -v`, which deletes all data), then update `.env`.
- The first two commits still contain the old placeholder values (`postgres` password, a dev JWT key). The local database password was rotated, so they are useless; rewrite history before publishing if that matters.
