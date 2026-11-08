import { randomUUID } from 'node:crypto'
import { Temporal } from 'temporal-polyfill'
import type { TimeRecordSources } from '../application/timeRecords.js'

export const timeRecordSources: TimeRecordSources = {
  newId: () => randomUUID(),
  now: () => Temporal.Instant.fromEpochMilliseconds(Date.now()),
}
