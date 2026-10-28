import Link from 'next/link'

// Shown when the page calls notFound(): the student doesn't exist, or
// isn't yours to see. The same words for both, on purpose.
export default function StudentNotFound() {
  return (
    <section className="space-y-2">
      <h1 className="text-xl font-semibold">Student not found</h1>
      <p className="text-muted-foreground">There is no student at this address that you can see.</p>
      <Link href="/" className="text-sm underline underline-offset-4">
        Back to your dashboard
      </Link>
    </section>
  )
}
