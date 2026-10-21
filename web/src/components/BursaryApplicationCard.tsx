import { useState } from 'react'
import type { BursaryCardData } from '@/api/types'
import { DeadlineCountdown } from '@/components/DeadlineCountdown'
import { StatusBadge } from '@/components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatDate } from '@/lib/dates'
import { formatCents } from '@/lib/money'

type BursaryApplicationCardProps = {
  application: BursaryCardData
}

export function BursaryApplicationCard({ application }: BursaryApplicationCardProps) {
  const [showDetails, setShowDetails] = useState(false)

  return (
    <Card>
      <CardHeader>
        {/* shadcn's CardTitle is a <div>. The <h3> keeps the heading that
            screen-reader users jump between. */}
        <CardTitle>
          <h3>{application.funder}</h3>
        </CardTitle>
        <CardAction>
          <StatusBadge status={application.status} />
        </CardAction>
      </CardHeader>

      <CardContent className="space-y-3">
        <p className="text-2xl font-semibold tabular-nums">
          {formatCents(application.amountInCents)}
        </p>
        <DeadlineCountdown deadline={application.deadline} />

        <Button variant="outline" size="sm" onClick={() => setShowDetails(!showDetails)}>
          {showDetails ? 'Hide details' : 'Show details'}
        </Button>

        {showDetails && (
          <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-sm">
            <dt className="text-muted-foreground">Deadline</dt>
            <dd>{formatDate(application.deadline)}</dd>
            <dt className="text-muted-foreground">Reference</dt>
            <dd className="font-mono text-xs">{application.id}</dd>
          </dl>
        )}
      </CardContent>
    </Card>
  )
}
