import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import type { TimeRecordPresentation } from '../presentation/timeRecordPresentation'
import type { TimeRecordService } from '../presentation/timeRecordService'

type TimeRecordResponse = {
  id: string
  startedAt: string
  endedAt: string | null
}

export const timeRecordService: TimeRecordService = {
  async getTimeRecords() {
    const response = await fetch('/api/time-records')
    const records: TimeRecordResponse[] = await response.json()
    return records.map(toTimeRecord)
  },
  async startTimeRecord() {
    const response = await fetch('/api/time-records/start', { method: 'POST' })
    return toTimeRecord(await response.json())
  },
  async endTimeRecord(id) {
    const response = await fetch(`/api/time-records/${id}/end`, { method: 'POST' })
    return toTimeRecord(await response.json())
  },
}

function toTimeRecord(response: TimeRecordResponse): TimeRecordPresentation {
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
