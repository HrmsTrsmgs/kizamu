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
      const body = await requestJson('/api/time-records', headers ? { headers } : undefined)
      if (!Array.isArray(body) || !body.every(isTimeRecordResponse)) {
        throw new Error()
      }
      const records: TimeRecordResponse[] = body
      return records.map(toTimeRecord)
    },
    startTimeRecord() {
      return postTimeRecord('/api/time-records/start', headers)
    },
    async endTimeRecord(id) {
      const record = await postTimeRecord(`/api/time-records/${id}/end`, headers)
      if (record.record.id !== id || record.record.endedAt === null) {
        throw new Error()
      }
      return record
    },
  }
}

async function postTimeRecord(path: string, headers: Record<string, string> | undefined): Promise<TimeRecordPresentation> {
  const body = await requestJson(path, headers ? { method: 'POST', headers } : { method: 'POST' })
  if (!isTimeRecordResponse(body)) {
    throw new Error()
  }
  return toTimeRecord(body)
}

async function requestJson(path: string, options?: RequestInit): Promise<unknown> {
  const response = options ? await fetch(path, options) : await fetch(path)
  if (!response.ok) {
    throw new Error()
  }
  return response.json()
}

function isTimeRecordResponse(value: unknown): value is TimeRecordResponse {
  return typeof value === 'object'
    && value !== null
    && 'id' in value
    && typeof value.id === 'string'
    && 'startedAt' in value
    && typeof value.startedAt === 'string'
    && 'endedAt' in value
    && (typeof value.endedAt === 'string' || value.endedAt === null)
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
