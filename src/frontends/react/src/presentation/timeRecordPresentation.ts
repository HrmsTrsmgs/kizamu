import type { TimeRecord } from 'kizamu-domain-typescript'

// Keep the original ISO notation for the HTML time element without reparsing it in the view.
export type TimeRecordPresentation = Readonly<{
  record: TimeRecord
  isoText: Readonly<{
    startedAt: string
    endedAt: string | null
  }>
}>
