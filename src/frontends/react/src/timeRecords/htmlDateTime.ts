import type { Temporal } from 'temporal-polyfill'

export function formatHtmlDateTime(instant: Temporal.Instant) {
  return instant.toString({ smallestUnit: 'millisecond', roundingMode: 'floor' })
}
