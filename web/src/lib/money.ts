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
