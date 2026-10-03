import type { TimeRecordApi } from '../../../../src/frontends/react/src/timeRecords/timeRecordApi'

export function createTimeRecordApiMock(
  overrides: Partial<TimeRecordApi> = {},
): TimeRecordApi {
  return {
    getTimeRecords: async () => [],
    startTimeRecord: async () => ({ id: '', startedAt: '', endedAt: null }),
    endTimeRecord: async () => ({ id: '', startedAt: '', endedAt: '' }),
    ...overrides,
  }
}
