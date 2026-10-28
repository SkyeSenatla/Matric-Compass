import Link from 'next/link'
import { Suspense } from 'react'
import { logout } from '@/app/actions/auth'
import { Button } from '@/components/ui/button'
import { getSession } from '@/lib/session'

// (app) is a route group: every signed-in page shares this sidebar layout,
// and "(app)" never appears in a URL. The layout stays mounted while you
// move between its pages.
export default function SignedInLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-svh bg-background text-foreground md:grid md:grid-cols-[15rem_1fr]">
      <aside className="space-y-6 border-b p-4 md:border-r md:border-b-0">
        <p className="text-lg font-semibold">Matric Compass</p>
        {/* Reading the session cookie is request-time work, so it waits
            inside Suspense while the rest of the layout is sent at once. */}
        <Suspense fallback={<p className="text-sm text-muted-foreground">Loading…</p>}>
          <SidebarNav />
        </Suspense>
      </aside>
      <main className="min-w-0 px-4 py-6">{children}</main>
    </div>
  )
}

async function SidebarNav() {
  const session = await getSession()
  if (session === null) return null

  // Links chosen by role are a convenience, not security: the API still
  // refuses a learner who opens /students (403), whatever this shows.
  const links =
    session.role === 'Learner'
      ? [{ href: '/me', label: 'My dashboard' }]
      : [
          { href: '/dashboard/counsellor', label: 'Dashboard' },
          { href: '/students', label: 'Students' },
        ]

  return (
    <nav aria-label="Main" className="space-y-6">
      <ul className="space-y-1">
        {links.map((link) => (
          <li key={link.href}>
            <Link href={link.href} className="block rounded-md px-2 py-1 text-sm hover:bg-muted">
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
      <div className="space-y-2 text-sm">
        <p className="break-words text-muted-foreground">
          {session.email} · {session.role}
        </p>
        {/* A form whose action is a Server Action: no JavaScript needed. */}
        <form action={logout}>
          <Button type="submit" variant="outline" size="sm">
            Sign out
          </Button>
        </form>
      </div>
    </nav>
  )
}
