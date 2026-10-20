import { useState } from 'react'
import type { DashboardBursaryApplication } from '../api/types'
import { formatDate } from '../lib/dates'
import { formatCents } from '../lib/money'

type BursaryApplicationCardProps = {
  application: DashboardBursaryApplication
}

export function BursaryApplicationCard({ application }: BursaryApplicationCardProps) {
  const [showDetails, setShowDetails] = useState(false)

  return (
    <article className="card">
      <h3>{application.funder}</h3>
      <p className="amount">{formatCents(application.amountInCents)}</p>
      <p>
        Status: <strong>{application.status}</strong>
      </p>
      <p>{describeDeadline(application.daysUntilDeadline)}</p>

      <button type="button" onClick={() => setShowDetails(!showDetails)}>
        {showDetails ? 'Hide details' : 'Show details'}
      </button>

      {showDetails && (
        <dl>
          <dt>Deadline</dt>
          <dd>{formatDate(application.deadline)}</dd>
          <dt>Reference</dt>
          <dd>{application.id}</dd>
        </dl>
      )}
    </article>
  )
}

// The server worked out daysUntilDeadline with its own clock, so a phone with
// the wrong date can't make a deadline look further away than it is.
function describeDeadline(days: number): string {
  if (days < 0) return `Deadline passed ${Math.abs(days)} day(s) ago`
  if (days === 0) return 'Deadline is today'
  return `Due in ${days} day(s)`
}
