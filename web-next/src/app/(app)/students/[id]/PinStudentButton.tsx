'use client'

import { Button } from '@/components/ui/button'
import { usePinnedStudents } from '@/stores/PinnedStudentsProvider'

// Only the id and the name go into the store: enough for a link, and nothing
// the API owns (marks, applications) gets a second copy that could go stale.
export function PinStudentButton({ id, name }: { id: string; name: string }) {
  const isPinned = usePinnedStudents((state) => state.pinned.some((p) => p.id === id))
  const pin = usePinnedStudents((state) => state.pin)
  const unpin = usePinnedStudents((state) => state.unpin)

  return (
    <Button type="button" variant="outline" size="sm" aria-pressed={isPinned} onClick={() => (isPinned ? unpin(id) : pin({ id, name }))}>
      {isPinned ? `Unpin ${name}` : `Pin ${name}`}
    </Button>
  )
}
