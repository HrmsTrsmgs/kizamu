import type { Temporal } from 'temporal-polyfill'

export function calculateTimeRecordDuration(start: Temporal.Instant, end: Temporal.Instant): Temporal.Duration {
  return end.since(start, { largestUnit: 'hour' })
}
