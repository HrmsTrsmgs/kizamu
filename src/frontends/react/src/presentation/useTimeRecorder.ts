import { useEffect, useReducer, useRef } from 'react'
import type { TimeRecordService } from './timeRecordService'
import type { TimeRecordPresentation } from './timeRecordPresentation'
import { initialTimeRecorderState, timeRecorderReducer } from './timeRecorderState'

export function useTimeRecorder(timeRecords: TimeRecordService) {
  const [state, dispatch] = useReducer(timeRecorderReducer, initialTimeRecorderState)
  const pendingLoads = useRef<TimeRecordPresentation[][]>([])

  useEffect(() => {
    async function getTimeRecords() {
      const updatedRecords: TimeRecordPresentation[] = []
      pendingLoads.current.push(updatedRecords)
      try {
        const records = await timeRecords.getTimeRecords()
        dispatch({ type: 'recordsLoaded', records, updatedRecords })
      } finally {
        pendingLoads.current = pendingLoads.current.filter(pending => pending !== updatedRecords)
      }
    }

    void getTimeRecords()
  }, [timeRecords])

  async function startTimeRecord() {
    const record = await timeRecords.startTimeRecord()
    pendingLoads.current.forEach(pending => pending.push(record))
    dispatch({ type: 'recordStarted', record })
  }

  async function endTimeRecord() {
    if (state.timeRecordId !== null) {
      const record = await timeRecords.endTimeRecord(state.timeRecordId)
      pendingLoads.current.forEach(pending => pending.push(record))
      dispatch({ type: 'recordEnded', record })
    }
  }

  return { state, startTimeRecord, endTimeRecord }
}
