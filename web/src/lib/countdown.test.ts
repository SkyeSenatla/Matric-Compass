import { describe, expect, it } from 'vitest'
import { describeCountdown, getCountdown, saCalendarDaysBetween } from './countdown'

const at = (iso: string) => Date.parse(iso)

describe('getCountdown', () => {
  it('is closed once the deadline has passed', () => {
    expect(getCountdown('2026-10-20T10:00:00Z', at('2026-10-20T10:00:01Z'))).toEqual({ kind: 'closed' })
  })

  it('is closed at the exact deadline', () => {
    expect(getCountdown('2026-10-20T10:00:00Z', at('2026-10-20T10:00:00Z'))).toEqual({ kind: 'closed' })
  })

  it('counts hours, minutes and seconds in the last day', () => {
    expect(getCountdown('2026-10-20T15:03:09Z', at('2026-10-20T10:00:00Z'))).toEqual({
      kind: 'hours',
      hours: 5,
      minutes: 3,
      seconds: 9,
    })
  })

  it('switches to days at exactly 24 hours', () => {
    expect(getCountdown('2026-10-21T10:00:00Z', at('2026-10-20T10:00:00Z'))).toEqual({ kind: 'days', days: 1 })
  })
})

describe('saCalendarDaysBetween', () => {
  it('counts South African calendar days, not UTC ones', () => {
    // 23:30 SAST on 20 Oct  ->  00:30 SAST on 23 Oct.
    // In UTC that's 20 Oct -> 22 Oct (2 days); in Johannesburg it's 3.
    expect(saCalendarDaysBetween(at('2026-10-20T21:30:00Z'), at('2026-10-22T22:30:00Z'))).toBe(3)
  })

  it('crosses midnight SAST as a whole day, even after one hour', () => {
    expect(saCalendarDaysBetween(at('2026-10-20T21:30:00Z'), at('2026-10-20T22:30:00Z'))).toBe(1)
  })
})

describe('describeCountdown', () => {
  it('describes each kind in words', () => {
    expect(describeCountdown({ kind: 'closed' })).toBe('Closed')
    expect(describeCountdown({ kind: 'hours', hours: 5, minutes: 3, seconds: 9 })).toBe('Closes in 05:03:09')
    expect(describeCountdown({ kind: 'days', days: 1 })).toBe('Due tomorrow')
    expect(describeCountdown({ kind: 'days', days: 46 })).toBe('Due in 46 days')
  })
})
