/**
 * Fractional indexing for ordering blocks and pages without full reindexing.
 *
 * generatePosition('', '')   → 'a0' (first position)
 * generatePosition('a0', '') → 'b0' (after 'a0', no upper bound)
 * generatePosition('a0', 'b0') → 'aV' (between 'a0' and 'b0')
 */

const DIGITS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"

export function generatePosition(lower: string, upper: string): string {
  if (!lower && !upper) return "a0"
  if (!lower) return decrementPosition(upper)
  if (!upper) return incrementPosition(lower)

  // Pad to same length
  let l = lower
  let u = upper
  while (l.length < u.length) l += "0"
  while (u.length < l.length) u += String.fromCharCode(DIGITS.charCodeAt(0))

  if (l < u) {
    const mid = midpoint(l, u)
    if (mid) return mid
  }

  // Fallback: append midpoint character
  return lower + "V"
}

function midpoint(a: string, b: string): string | null {
  const lastA = DIGITS.indexOf(a[a.length - 1])
  const lastB = DIGITS.indexOf(b[b.length - 1])
  if (lastB - lastA >= 2) {
    const mid = Math.round((lastA + lastB) / 2)
    return a.slice(0, -1) + DIGITS[mid]
  }
  return null
}

export function incrementPosition(pos: string): string {
  const lastIdx = DIGITS.indexOf(pos[pos.length - 1])
  if (lastIdx < DIGITS.length - 1) {
    return pos.slice(0, -1) + DIGITS[lastIdx + 1]
  }
  return pos + DIGITS[0]
}

export function decrementPosition(pos: string): string {
  const firstIdx = DIGITS.indexOf(pos[0])
  if (firstIdx > 0) {
    return DIGITS[firstIdx - 1] + pos.slice(1)
  }
  return "0" + pos
}
