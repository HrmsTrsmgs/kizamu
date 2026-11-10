import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'
import { presentedRecord, timestamp } from './timeRecords'

const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')

export function createTimeRecordServiceStub(
  overrides: Partial<TimeRecordService> = {},
): TimeRecordService {
  return {
    getTimeRecords: async () => [],
    startTimeRecord: async () => presentedRecord('record', 開始時刻),
    endTimeRecord: async () => presentedRecord('record', 開始時刻, 終了時刻),
    ...overrides,
  }
}
