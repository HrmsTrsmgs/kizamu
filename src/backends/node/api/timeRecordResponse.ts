import { transformInto } from '@typesugar/mapper'
import type { TimeRecord } from 'kizamu-domain-typescript'
import type { Temporal } from 'temporal-polyfill'

type TimeRecordResponse = {
  id: string
  startedAt: string
  endedAt: string | null
}

export function toTimeRecordResponse(record: TimeRecord): TimeRecordResponse {
  return transformInto<TimeRecord, TimeRecordResponse>(record, {
    compute: {
      startedAt: ({ startedAt }) => formatInstant(startedAt),
      endedAt: ({ endedAt }) => endedAt === null ? null : formatInstant(endedAt),
    },
  })
}

function formatInstant(instant: Temporal.Instant): string {
  return instant.toString({ fractionalSecondDigits: 3 })
}
