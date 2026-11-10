import type { TimeRecordPresentation } from './timeRecordPresentation'

export type TimeRecordService = {
  getTimeRecords: () => Promise<readonly TimeRecordPresentation[]>
  startTimeRecord: () => Promise<TimeRecordPresentation>
  endTimeRecord: (id: string) => Promise<TimeRecordPresentation>
}
