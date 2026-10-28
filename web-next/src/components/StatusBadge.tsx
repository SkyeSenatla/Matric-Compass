import {
  CircleCheck,
  CircleHelp,
  CircleX,
  FileCheck,
  Hourglass,
  MailOpen,
  PencilLine,
  Search,
  Send,
  type LucideIcon,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'

type Tone = 'neutral' | 'info' | 'positive' | 'negative'

type StatusStyle = { label: string; icon: LucideIcon; tone: Tone }

// The ONE place that knows what each status looks like. Bursary statuses
// (Draft ... Rejected) and tertiary statuses (Researching ... Accepted) share
// the table; "Rejected" means the same thing in both.
const STATUS_STYLES: Record<string, StatusStyle> = {
  Draft: { label: 'Draft', icon: PencilLine, tone: 'neutral' },
  Submitted: { label: 'Submitted', icon: Send, tone: 'info' },
  UnderReview: { label: 'Under review', icon: Hourglass, tone: 'info' },
  Approved: { label: 'Approved', icon: CircleCheck, tone: 'positive' },
  Rejected: { label: 'Rejected', icon: CircleX, tone: 'negative' },
  Researching: { label: 'Researching', icon: Search, tone: 'neutral' },
  Applied: { label: 'Applied', icon: Send, tone: 'info' },
  DocumentsSubmitted: { label: 'Documents submitted', icon: FileCheck, tone: 'info' },
  Offered: { label: 'Offered', icon: MailOpen, tone: 'info' },
  Accepted: { label: 'Accepted', icon: CircleCheck, tone: 'positive' },
}

// Each tone maps to one of shadcn's Badge variants.
const TONE_VARIANTS = {
  neutral: 'outline',
  info: 'secondary',
  positive: 'secondary',
  negative: 'destructive',
} as const

// Our own additions on top of shadcn's variants:
// - positive: shadcn has no "success" variant, so we add our own green.
// - negative: shadcn's destructive text measured 4.01:1 contrast on its pale
//   red background in light mode. Badge text is small (12px), which needs
//   4.5:1 (WCAG 2.2 SC 1.4.3), so we darken it.
const TONE_CLASSES: Partial<Record<Tone, string>> = {
  positive: 'bg-emerald-600/10 text-emerald-800 dark:bg-emerald-400/15 dark:text-emerald-300',
  negative: 'text-red-700 dark:text-red-300',
}

type StatusBadgeProps = {
  status: string
}

export function StatusBadge({ status }: StatusBadgeProps) {
  // The generated type says `string`, so the API could send a value we've
  // never seen. Show it as it came, in a neutral badge: no crash, no guess.
  const style = STATUS_STYLES[status] ?? { label: status, icon: CircleHelp, tone: 'neutral' }
  const Icon = style.icon

  // Colour is never the only signal: every badge has a word and an icon too
  // (WCAG 2.2 SC 1.4.1).
  return (
    <Badge variant={TONE_VARIANTS[style.tone]} className={TONE_CLASSES[style.tone]}>
      <Icon />
      {style.label}
    </Badge>
  )
}
