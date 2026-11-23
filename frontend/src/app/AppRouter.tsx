import { Navigate, Route, Routes } from 'react-router-dom'
import {
  ForgotPasswordPage,
  LoginPage,
  RegisterPage,
  RequireAuth,
  ResetPasswordPage,
  SessionsPage,
  SettingsPage,
} from '@/features/auth'
import { MessagesPage, OpenChatPage } from '@/features/chat'
import { ContactsPage } from '@/features/friends'
import { JabberDashboardPage, JabberInfoCard, RequireAdmin } from '@/features/jabber'
import { PrivateRoomsPage, RoomCatalogPage, RoomPage } from '@/features/rooms'
import HomePage from '@/pages/HomePage'
import AppLayout from './AppLayout'
import ChatLayout from './ChatLayout'

export default function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/contacts" element={<ContactsPage />} />
          <Route path="/sessions" element={<SessionsPage />} />
          <Route
            path="/settings"
            element={
              <SettingsPage>
                <JabberInfoCard />
              </SettingsPage>
            }
          />
          <Route element={<RequireAdmin />}>
            <Route path="/admin/jabber" element={<JabberDashboardPage />} />
          </Route>

          <Route element={<ChatLayout />}>
            <Route path="/messages" element={<MessagesPage />} />
            <Route path="/messages/with/:userId" element={<OpenChatPage />} />
            <Route path="/messages/:chatId" element={<MessagesPage />} />
            <Route path="/rooms" element={<RoomCatalogPage />} />
            <Route path="/rooms/private" element={<PrivateRoomsPage />} />
            <Route path="/rooms/:roomId" element={<RoomPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
