import { redirect } from 'next/navigation'
import { getSession } from '@/lib/session'

// "/" has no screen of its own: it sends you to yours. There's nothing to
// show while it decides, so it may block instead of streaming a shell.
export const instant = false

export default async function HomePage() {
  const session = await getSession()
  if (session === null) redirect('/login')
  redirect(session.role === 'Learner' ? '/me' : '/dashboard/counsellor')
}
