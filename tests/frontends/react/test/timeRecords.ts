import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import type { RecordTimestamp, TimeRecordPresentation } from '../../../../src/frontends/react/src/presentation/timeRecordPresentation'

export function timestamp(isoText: string): RecordTimestamp {
  return { instant: Temporal.Instant.from(isoText), isoText }
}

export function presentedRecord(id: string, startedAt: RecordTimestamp, endedAt: RecordTimestamp | null = null): TimeRecordPresentation {
  return {
    record: new TimeRecord(id, startedAt.instant, endedAt?.instant ?? null),
    isoText: { startedAt: startedAt.isoText, endedAt: endedAt?.isoText ?? null },
  }
}
