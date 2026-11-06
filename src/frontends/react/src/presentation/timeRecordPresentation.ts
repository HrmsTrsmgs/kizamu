import type { Temporal } from 'temporal-polyfill'
import type { TimeRecord } from 'kizamu-domain-typescript'

// Keep the original ISO notation for the HTML time element without reparsing it in the view.
export type RecordTimestamp = Readonly<{
  instant: Temporal.Instant
  isoText: string
}>

export type TimeRecordPresentation = Readonly<{
  record: TimeRecord
  isoText: Readonly<{
    startedAt: string
    endedAt: string | null
  }>
}>

export function getTimeRecordTimestamps({ record, isoText }: TimeRecordPresentation) {
  return {
    startedAt: { instant: record.startedAt, isoText: isoText.startedAt },
    endedAt: record.endedAt === null || isoText.endedAt === null ? null : {
      instant: record.endedAt,
      isoText: isoText.endedAt,
    },
  }
}
