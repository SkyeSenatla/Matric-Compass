import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

// The order a tertiary application moves through. Accepted and Rejected are
// both final, so they share the last step.
const STEPS = [
  { key: 'Researching', label: 'Researching' },
  { key: 'Applied', label: 'Applied' },
  { key: 'DocumentsSubmitted', label: 'Documents submitted' },
  { key: 'Offered', label: 'Offered' },
  { key: 'Decision', label: 'Decision' },
] as const

type ApplicationPipelineProps = {
  status: string
}

export function ApplicationPipeline({ status }: ApplicationPipelineProps) {
  const isFinal = status === 'Accepted' || status === 'Rejected'
  const currentIndex = STEPS.findIndex((step) => step.key === (isFinal ? 'Decision' : status))

  // An unknown status still shows in the card's badge; we just skip the steps.
  if (currentIndex === -1) return null

  return (
    <ol className="space-y-1 text-sm">
      {STEPS.map((step, index) => {
        const done = index < currentIndex
        const current = index === currentIndex
        return (
          <li
            key={step.key}
            className={cn(
              'flex items-center gap-2',
              current ? 'font-semibold text-foreground' : 'text-muted-foreground',
            )}
            // Screen readers announce this as "current step": the colour and
            // weight alone would only tell sighted users.
            aria-current={current ? 'step' : undefined}
          >
            <span
              className={cn(
                'flex size-5 shrink-0 items-center justify-center rounded-full border text-xs',
                (done || current) && 'border-primary bg-primary text-primary-foreground',
              )}
            >
              {done ? <Check className="size-3" /> : index + 1}
            </span>
            {step.key === 'Decision' && isFinal ? status : step.label}
          </li>
        )
      })}
    </ol>
  )
}
