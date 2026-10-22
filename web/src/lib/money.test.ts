import { describe, expect, it } from 'vitest'
import { formatCents, parseRandsToCents } from './money'

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

describe('parseRandsToCents', () => {
  it('reads rands and cents exactly', () => {
    expect(parseRandsToCents('150.50')).toBe(15_050)
    expect(parseRandsToCents('150')).toBe(15_000)
  })

  it('reads one decimal digit as tens of cents', () => {
    expect(parseRandsToCents('150.5')).toBe(15_050)
  })

  it('gets the floating-point trap amounts right', () => {
    // 19.99 * 100 = 1998.9999999999998 and 0.29 * 100 = 28.999999999999996
    expect(parseRandsToCents('19.99')).toBe(1_999)
    expect(parseRandsToCents('0.29')).toBe(29)
  })

  it('accepts a decimal comma, spaces between thousands and an R', () => {
    expect(parseRandsToCents('1 500,50')).toBe(150_050)
    expect(parseRandsToCents(`1${NBSP}500,50`)).toBe(150_050)
    expect(parseRandsToCents('R 250')).toBe(25_000)
    expect(parseRandsToCents('  45000  ')).toBe(4_500_000)
  })

  it('refuses to round: more than two decimal places is an error', () => {
    expect(parseRandsToCents('1.005')).toBeNull()
  })

  it('refuses to guess what a comma AND a point mean', () => {
    expect(parseRandsToCents('1,500.50')).toBeNull()
  })

  it('refuses anything that is not an amount', () => {
    for (const input of ['', 'abc', '-5', '12e3', '1.', '.50', '1 0 0 0 0 0 0 0 0 0']) {
      expect(parseRandsToCents(input)).toBeNull()
    }
  })

  it('returns zero for zero: the form, not the parser, decides that is not allowed', () => {
    expect(parseRandsToCents('0')).toBe(0)
  })
})
