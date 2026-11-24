import { autoMap, createMap, createMapper, forMember, mapFrom } from '@automapper/core'
import { pojos } from '@automapper/pojos'
import type { TimeRecord } from 'kizamu-domain-typescript'
import type { Temporal } from 'temporal-polyfill'

type TimeRecordResponse = {
  id: string
  startedAt: string
  endedAt: string | null
}

const timeRecordModel = 'kizamu-backend-node/TimeRecord'
const timeRecordResponseModel = 'kizamu-backend-node/TimeRecordResponse'
const mapper = createMapper({ strategyInitializer: pojos() })

createMap<TimeRecord, TimeRecordResponse>(
  mapper,
  timeRecordModel,
  timeRecordResponseModel,
  autoMap('id'),
  forMember(response => response.startedAt, mapFrom(record => formatInstant(record.startedAt))),
  forMember(
    response => response.endedAt,
    mapFrom(({ endedAt }) => endedAt === null ? null : formatInstant(endedAt)),
  ),
)

export function toTimeRecordResponse(record: TimeRecord): TimeRecordResponse {
  return mapper.map<TimeRecord, TimeRecordResponse>(record, timeRecordModel, timeRecordResponseModel)
}

function formatInstant(instant: Temporal.Instant): string {
  return instant.toString({ fractionalSecondDigits: 3 })
}
