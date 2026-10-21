import { useEffect, useState } from 'react'
import { Clock } from 'lucide-react'
import { describeCountdown, getCountdown } from '@/lib/countdown'

type DeadlineCountdownProps = {
  deadline: string
}

export function DeadlineCountdown({ deadline }: DeadlineCountdownProps) {
  // The current time is data from OUTSIDE React. We keep a copy in state and
  // let an effect refresh it.
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    // Setup: start a timer that updates `now` once a second.
    const id = setInterval(() => setNow(Date.now()), 1000)

    // Cleanup: stop exactly the timer this setup started. React runs this
    // when the card disappears, and between StrictMode's two mounts.
    return () => clearInterval(id)

    // No dependencies: the effect reads no props or state (setNow never
    // changes), so it never needs to restart.
  }, [])

  // Derived on every render from the deadline and the latest `now`.
  const text = describeCountdown(getCountdown(deadline, now))

  return (
    <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
      <Clock className="size-4" />
      {/* No aria-live here: a screen reader announcing every second would be
          unbearable. The full deadline date is on the card for everyone. */}
      <time dateTime={deadline}>{text}</time>
    </p>
  )
}
