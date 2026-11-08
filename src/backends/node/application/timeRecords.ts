import { TimeRecord } from 'kizamu-domain-typescript'
import type { Temporal } from 'temporal-polyfill'

export interface TimeRecordState {
  add(record: TimeRecord): void
  find(id: string): TimeRecord | undefined
  replace(record: TimeRecord): void
  all(): readonly TimeRecord[]
}

export interface TimeRecordSources {
  newId(): string
  now(): Temporal.Instant
}

export class TimeRecords {
  constructor(private readonly state: TimeRecordState, private readonly sources: TimeRecordSources) {}

  start(): TimeRecord {
    const record = TimeRecord.start(this.sources.newId(), this.sources.now())
    this.state.add(record)
    return record
  }

  end(id: string): TimeRecord | undefined {
    const record = this.state.find(id)
    if (record !== undefined) {
      const ended = record.end(this.sources.now())
      this.state.replace(ended)
      return ended
    }
  }

  list(): readonly TimeRecord[] {
    return this.state.all()
  }
}
