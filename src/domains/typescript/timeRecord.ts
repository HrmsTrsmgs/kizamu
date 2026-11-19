import type { Temporal } from 'temporal-polyfill'

export class TimeRecord {
  constructor(
    readonly id: string,
    readonly startedAt: Temporal.Instant,
    readonly endedAt: Temporal.Instant | null,
  ) {}

  static start(id: string, startedAt: Temporal.Instant): TimeRecord {
    return new TimeRecord(id, startedAt, null)
  }

  durationAt(asOf: Temporal.Instant): Temporal.Duration {
    return (this.endedAt ?? asOf).since(this.startedAt, { largestUnit: 'hour' })
  }

  end(endedAt: Temporal.Instant): TimeRecord {
    return new TimeRecord(this.id, this.startedAt, endedAt)
  }
}
