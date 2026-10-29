'use client'

import { useQueryStates } from 'nuqs'
import { useTransition } from 'react'
import { Label } from '@/components/ui/label'
import { NativeSelect, NativeSelectOption } from '@/components/ui/native-select'
import { bursaryFilters, sortOrders, statuses } from '@/lib/bursaryFilters'

const statusLabels: Record<(typeof statuses)[number], string> = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  UnderReview: 'Under review',
  Approved: 'Approved',
  Rejected: 'Rejected',
}

// useState, except the value lives in the URL. shallow: false tells the
// server too, so the page's first list is rendered again for the new filter.
// The transition says when that new list has arrived.
export function BursaryFilters() {
  const [isUpdating, startTransition] = useTransition()
  const [{ status, sort }, setFilters] = useQueryStates(bursaryFilters, { shallow: false, startTransition })

  return (
    <div className="flex flex-wrap items-end gap-4">
      <div className="space-y-1">
        <Label htmlFor="filter-status">Status</Label>
        <NativeSelect
          id="filter-status"
          value={status ?? ''}
          onChange={(e) => void setFilters({ status: e.target.value === '' ? null : (e.target.value as typeof status) })}
        >
          <NativeSelectOption value="">Every status</NativeSelectOption>
          {statuses.map((s) => (
            <NativeSelectOption key={s} value={s}>
              {statusLabels[s]}
            </NativeSelectOption>
          ))}
        </NativeSelect>
      </div>
      <div className="space-y-1">
        <Label htmlFor="filter-sort">Sort by</Label>
        <NativeSelect id="filter-sort" value={sort} onChange={(e) => void setFilters({ sort: e.target.value as typeof sort })}>
          {sortOrders.map((s) => (
            <NativeSelectOption key={s} value={s}>
              {s === 'Deadline' ? 'Soonest deadline' : 'Smallest amount'}
            </NativeSelectOption>
          ))}
        </NativeSelect>
      </div>
      <p aria-live="polite" className="pb-2 text-sm text-muted-foreground">
        {isUpdating ? 'Updating…' : ''}
      </p>
    </div>
  )
}
