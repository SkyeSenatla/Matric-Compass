import { useState } from 'react'
import { ArrowUpDown } from 'lucide-react'
import type { BursaryCardData } from '@/api/types'
import { BursaryApplicationCard } from '@/components/BursaryApplicationCard'
import { Button } from '@/components/ui/button'

type BursaryApplicationListProps = {
  applications: BursaryCardData[]
}

// Owns the sort order: only this list and its button use it, so this is its
// lowest common owner. App no longer knows the list can be sorted.
export function BursaryApplicationList({ applications }: BursaryApplicationListProps) {
  const [soonestFirst, setSoonestFirst] = useState(true)

  // Sort by the deadline itself, not by daysUntilDeadline: the countdown on
  // each card ticks live, and the order should agree with it.
  const sorted = applications.toSorted((a, b) => {
    const difference = Date.parse(a.deadline) - Date.parse(b.deadline)
    return soonestFirst ? difference : -difference
  })

  if (sorted.length === 0) {
    return <p className="text-muted-foreground">No bursary applications yet.</p>
  }

  return (
    <div className="space-y-4">
      <Button variant="outline" size="sm" onClick={() => setSoonestFirst(!soonestFirst)}>
        <ArrowUpDown />
        {soonestFirst ? 'Soonest deadline first' : 'Latest deadline first'}
      </Button>

      {/* Mobile first: one column by default, two from the sm breakpoint
          (640px), three from lg (1024px). */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {sorted.map((application) => (
          <BursaryApplicationCard key={application.id} application={application} />
        ))}
      </div>
    </div>
  )
}
