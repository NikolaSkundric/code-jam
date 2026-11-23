import AddFriendForm from '../components/AddFriendForm'
import BannedUsersSection from '../components/BannedUsersSection'
import FriendRequestsSection from '../components/FriendRequestsSection'
import FriendsList from '../components/FriendsList'
import './contacts.css'

export default function ContactsPage() {
  return (
    <section className="contacts-page">
      <h1>Contacts</h1>
      <FriendsList />
      <FriendRequestsSection />
      <AddFriendForm />
      <BannedUsersSection />
    </section>
  )
}
