import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import type { TimeRecordPresentation } from '../presentation/timeRecordPresentation'
import type { TimeRecordService } from '../presentation/timeRecordService'

type TimeRecordResponse = {
  id: string
  startedAt: string
  endedAt: string | null
}

export const timeRecordService = createTimeRecordService()

export function createTimeRecordService(token?: string): TimeRecordService {
  const headers = token === undefined ? undefined : { Authorization: `Bearer ${token}` }
  return {
    async getTimeRecords() {
      const response = headers
        ? await fetch('/api/time-records', { headers })
        : await fetch('/api/time-records')
      if (!response.ok) {
        throw new Error()
      }
      const records: TimeRecordResponse[] = await response.json()
      return records.map(toTimeRecord)
    },
    startTimeRecord() {
      return postTimeRecord('/api/time-records/start', headers)
    },
    endTimeRecord(id) {
      return postTimeRecord(`/api/time-records/${id}/end`, headers)
    },
  }
}

async function postTimeRecord(path: string, headers: Record<string, string> | undefined): Promise<TimeRecordPresentation> {
  const response = await fetch(path, headers ? { method: 'POST', headers } : { method: 'POST' })
  if (!response.ok) {
    throw new Error()
  }
  return toTimeRecord(await response.json())
}

function toTimeRecord(response: TimeRecordResponse): TimeRecordPresentation {
  if (typeof response.id !== 'string') {
    throw new Error()
  }
  return {
    record: new TimeRecord(
      response.id,
      Temporal.Instant.from(response.startedAt),
      response.endedAt === null ? null : Temporal.Instant.from(response.endedAt),
    ),
    isoText: {
      startedAt: response.startedAt,
      endedAt: response.endedAt,
    },
  }
}
