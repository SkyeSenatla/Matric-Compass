import { describe, expect, it } from 'vitest'
import { formatCents } from './money'

// Intl uses a NO-BREAK SPACE (U+00A0) after the "R" and between thousands,
// so the expected strings spell it out instead of using a normal space.
const NBSP = String.fromCharCode(0xa0)

describe('formatCents', () => {
  it('shows ten cents as R 0,10', () => {
    expect(formatCents(10)).toBe(`R${NBSP}0,10`)
  })

  it('shows zero as R 0,00', () => {
    expect(formatCents(0)).toBe(`R${NBSP}0,00`)
  })

  it('groups thousands and keeps the cents exact', () => {
    expect(formatCents(4_500_010)).toBe(`R${NBSP}45${NBSP}000,10`)
  })

  it('formats a negative amount', () => {
    expect(formatCents(-150)).toBe(`-R${NBSP}1,50`)
  })

  it('refuses a fraction of a cent', () => {
    expect(() => formatCents(10.5)).toThrow(RangeError)
  })
})
