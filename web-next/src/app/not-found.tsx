import Link from 'next/link'

// Any URL that matches no page.
export default function NotFound() {
  return (
    <main className="mx-auto max-w-xl space-y-2 px-4 py-16">
      <h1 className="text-xl font-semibold">Page not found</h1>
      <Link href="/" className="text-sm underline underline-offset-4">
        Go to your dashboard
      </Link>
    </main>
  )
}
