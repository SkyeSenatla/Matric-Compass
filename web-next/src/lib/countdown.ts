const HOUR = 60 * 60 * 1000
const DAY = 24 * HOUR

// "2026-10-20" for the calendar day this instant falls on IN JOHANNESBURG.
// en-CA formats dates as YYYY-MM-DD, which is easy to take apart.
const saCalendarDay = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Africa/Johannesburg',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

export type Countdown =
  | { kind: 'closed' }
  | { kind: 'hours'; hours: number; minutes: number; seconds: number }
  | { kind: 'days'; days: number }

// Pure: the same deadline and the same "now" always give the same answer.
// The component supplies "now"; this function never reads the clock itself,
// which is what makes it easy to test.
export function getCountdown(deadlineIso: string, nowMs: number): Countdown {
  const deadlineMs = Date.parse(deadlineIso)
  const remainingMs = deadlineMs - nowMs

  if (remainingMs <= 0) return { kind: 'closed' }

  if (remainingMs < DAY) {
    const totalSeconds = Math.floor(remainingMs / 1000)
    return {
      kind: 'hours',
      hours: Math.floor(totalSeconds / 3600),
      minutes: Math.floor((totalSeconds % 3600) / 60),
      seconds: totalSeconds % 60,
    }
  }

  return { kind: 'days', days: saCalendarDaysBetween(nowMs, deadlineMs) }
}

// Whole calendar days between two instants, counted on South African dates.
// 23:30 on the 20th to 00:30 on the 21st is 1 day, even though it's 1 hour.
export function saCalendarDaysBetween(fromMs: number, toMs: number): number {
  return dayNumber(toMs) - dayNumber(fromMs)
}

function dayNumber(ms: number): number {
  const [year, month, day] = saCalendarDay.format(ms).split('-').map(Number)
  return Date.UTC(year, month - 1, day) / DAY
}

export function describeCountdown(countdown: Countdown): string {
  switch (countdown.kind) {
    case 'closed':
      return 'Closed'
    case 'hours': {
      const pad = (n: number) => String(n).padStart(2, '0')
      return `Closes in ${pad(countdown.hours)}:${pad(countdown.minutes)}:${pad(countdown.seconds)}`
    }
    case 'days':
      return countdown.days === 1 ? 'Due tomorrow' : `Due in ${countdown.days} days`
  }
}
