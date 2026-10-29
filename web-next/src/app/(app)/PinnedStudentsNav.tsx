'use client'

import Link from 'next/link'
import { usePinnedStudents } from '@/stores/PinnedStudentsProvider'

// The sidebar's short list. Empty on the server and on the first render;
// the saved pins appear once the store has loaded them.
export function PinnedStudentsNav() {
  const pinned = usePinnedStudents((state) => state.pinned)
  if (pinned.length === 0) return null

  return (
    <nav aria-label="Pinned students" className="space-y-1">
      <p className="px-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">Pinned</p>
      <ul className="space-y-1">
        {pinned.map((student) => (
          <li key={student.id}>
            <Link href={`/students/${student.id}`} className="block rounded-md px-2 py-1 text-sm hover:bg-muted">
              {student.name}
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  )
}
