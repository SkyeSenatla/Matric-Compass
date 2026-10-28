import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

type CardSkeletonsProps = {
  count?: number
  label: string
}

// Shaped like the real cards (title, badge, amount, a line, a button), so
// nothing jumps when the data arrives.
export function CardSkeletons({ count = 3, label }: CardSkeletonsProps) {
  return (
    // aria-busy + a label: a screen reader hears "Loading bursary
    // applications", not a list of empty boxes.
    <div aria-busy="true" aria-label={label} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }, (_, index) => (
        // Placeholders have no data and never reorder, so the index is a
        // safe key here, and only here.
        <Card key={index}>
          <CardHeader className="flex flex-row items-center justify-between">
            <Skeleton className="h-5 w-32" />
            <Skeleton className="h-5 w-20 rounded-full" />
          </CardHeader>
          <CardContent className="space-y-3">
            <Skeleton className="h-8 w-40" />
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-7 w-24" />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
