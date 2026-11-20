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

/** 終了を要求した作業記録が保存先に存在しないことを示します。 */
export class TimeRecordNotFoundError extends Error {}

export class TimeRecords {
  constructor(private readonly state: TimeRecordState, private readonly sources: TimeRecordSources) {}

  start(): TimeRecord {
    const record = TimeRecord.start(this.sources.newId(), this.sources.now())
    this.state.add(record)
    return record
  }

  /** 指定した記録を終了します。保存先に存在しない場合は TimeRecordNotFoundError を投げます。 */
  end(id: string): TimeRecord {
    const record = this.state.find(id)
    if (!record) throw new TimeRecordNotFoundError()
    const ended = record.end(this.sources.now())
    this.state.replace(ended)
    return ended
  }

  list(): readonly TimeRecord[] {
    return this.state.all()
  }
}
