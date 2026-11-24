import type { TimeRecord } from 'kizamu-domain-typescript'

export type TimeRecordPresentation = Readonly<{
  record: TimeRecord
  // Preserve the ISO timestamp text received from the API.
  isoText: Readonly<{
    startedAt: string
    endedAt: string | null
  }>
}>
