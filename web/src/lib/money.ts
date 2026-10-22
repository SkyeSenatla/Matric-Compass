const zar = new Intl.NumberFormat('en-ZA', { style: 'currency', currency: 'ZAR' })

// The API sends money as whole cents. We never turn cents into a fractional
// floating-point number like 0.1: we build an exact decimal STRING ("0.10")
// and Intl.NumberFormat formats that string exactly as written.
export function formatCents(cents: number): string {
  if (!Number.isSafeInteger(cents)) {
    throw new RangeError(`Expected a whole number of cents, got ${cents}`)
  }

  const sign = cents < 0 ? '-' : ''
  const absolute = Math.abs(cents)
  const remainder = absolute % 100
  // A multiple of 100 divided by 100 is always a whole number, so this is exact.
  const rands = (absolute - remainder) / 100

  const decimal = `${sign}${rands}.${String(remainder).padStart(2, '0')}`
  return zar.format(decimal as `${number}`)
}

// Day 4: the reverse. Rands a person typed become whole cents, WITHOUT
// floating-point arithmetic: 19.99 * 100 is 1998.9999999999998 in JavaScript.
// We read the digits as text and only ever multiply whole numbers.
//
// Accepts the ways South Africans write an amount: "1500", "1500.5",
// "1 500,50", "R1500". Returns null for anything else, including more than
// two decimal places (we never round money) and "1,500.50" (is the comma a
// thousands separator or the decimal point? We refuse to guess).
export function parseRandsToCents(input: string): number | null {
  const text = input
    .trim()
    .replace(/^R\s*/i, '') // an optional "R" in front
    .replace(/\s/g, '') // spaces between thousands (\s includes the no-break space)
    .replace(',', '.') // a decimal comma becomes a decimal point

  const match = /^(\d{1,9})(?:\.(\d{1,2}))?$/.exec(text)
  if (match === null) return null

  const [, rands, cents = ''] = match
  // "5" after the point means 50 cents, not 5: pad it on the right.
  return Number(rands) * 100 + Number(cents.padEnd(2, '0'))
}
