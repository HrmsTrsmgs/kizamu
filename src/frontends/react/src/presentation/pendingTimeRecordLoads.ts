import type { TimeRecordPresentation } from './timeRecordPresentation'

export class PendingTimeRecordLoads {
  private readonly updatesByLoad = new Set<TimeRecordPresentation[]>()

  start(): { updatedRecords: readonly TimeRecordPresentation[], finish: () => void } {
    const updatedRecords: TimeRecordPresentation[] = []
    this.updatesByLoad.add(updatedRecords)
    return {
      updatedRecords,
      finish: () => { this.updatesByLoad.delete(updatedRecords) },
    }
  }

  recordUpdate(record: TimeRecordPresentation) {
    for (const updatedRecords of this.updatesByLoad) updatedRecords.push(record)
  }
}
