import { useEffect, useReducer } from 'react'
import type { TimeRecordService } from './timeRecordService'
import { initialTimeRecorderState, timeRecorderReducer } from './timeRecorderState'

export function useTimeRecorder(timeRecords: TimeRecordService) {
  const [state, dispatch] = useReducer(timeRecorderReducer, initialTimeRecorderState)

  useEffect(() => {
    async function getTimeRecords() {
      const records = await timeRecords.getTimeRecords()
      dispatch({ type: 'recordsLoaded', records })
    }

    void getTimeRecords()
  }, [timeRecords])

  async function startTimeRecord() {
    const record = await timeRecords.startTimeRecord()
    dispatch({ type: 'recordStarted', record })
  }

  async function endTimeRecord() {
    if (state.timeRecordId !== null) {
      const record = await timeRecords.endTimeRecord(state.timeRecordId)
      dispatch({ type: 'recordEnded', record })
    }
  }

  return { state, startTimeRecord, endTimeRecord }
}
