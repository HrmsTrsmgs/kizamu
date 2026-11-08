import type { TimeRecord } from 'kizamu-domain-typescript'
import type { TimeRecordState } from '../application/timeRecords.js'

export class InMemoryTimeRecordState implements TimeRecordState {
  private readonly records: TimeRecord[] = []

  add(record: TimeRecord): void {
    this.records.push(record)
  }

  find(id: string): TimeRecord | undefined {
    return this.records.find((record) => record.id === id)
  }

  replace(record: TimeRecord): void {
    const index = this.records.findIndex((existing) => existing.id === record.id)
    if (index !== -1) this.records[index] = record
  }

  all(): readonly TimeRecord[] {
    return this.records
  }
}
