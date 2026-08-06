import { Temporal } from 'temporal-polyfill'

export function calculateTimeRecordDuration(startedAt: string, endedAt: string): Temporal.Duration {
  const start = Temporal.Instant.from(startedAt)
  const end = Temporal.Instant.from(endedAt)

  return end.since(start, { largestUnit: 'hour' })
}
