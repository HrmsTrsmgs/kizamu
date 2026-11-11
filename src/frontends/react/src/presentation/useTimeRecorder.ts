import { useCallback, useEffect, useReducer, useRef, useState } from 'react'
import type { TimeRecordService } from './timeRecordService'
import type { TimeRecordPresentation } from './timeRecordPresentation'
import { initialTimeRecorderState, timeRecorderReducer } from './timeRecorderState'

export function useTimeRecorder(timeRecords: TimeRecordService) {
  const [state, dispatch] = useReducer(timeRecorderReducer, initialTimeRecorderState)
  const [isBusy, setIsBusy] = useState(false)
  const [isStateUnconfirmed, setIsStateUnconfirmed] = useState(false)
  const operationPending = useRef(false)
  const pendingLoads = useRef<TimeRecordPresentation[][]>([])

  const getTimeRecords = useCallback(async () => {
    const updatedRecords: TimeRecordPresentation[] = []
    pendingLoads.current.push(updatedRecords)
    try {
      const records = await timeRecords.getTimeRecords()
      dispatch({ type: 'recordsLoaded', records, updatedRecords })
      setIsStateUnconfirmed(false)
    } finally {
      pendingLoads.current = pendingLoads.current.filter(pending => pending !== updatedRecords)
    }
  }, [timeRecords])

  useEffect(() => {
    void getTimeRecords()
  }, [getTimeRecords])

  async function reloadTimeRecords() {
    if (operationPending.current) return
    operationPending.current = true
    setIsBusy(true)
    try {
      await getTimeRecords()
    } finally {
      operationPending.current = false
      setIsBusy(false)
    }
  }

  async function startTimeRecord() {
    if (operationPending.current || isStateUnconfirmed) return
    operationPending.current = true
    setIsBusy(true)
    try {
      const record = await timeRecords.startTimeRecord()
      pendingLoads.current.forEach(pending => pending.push(record))
      dispatch({ type: 'recordStarted', record })
    } catch (error) {
      try {
        await getTimeRecords()
      } catch (reloadError) {
        setIsStateUnconfirmed(true)
        throw reloadError
      }
      throw error
    } finally {
      operationPending.current = false
      setIsBusy(false)
    }
  }

  async function endTimeRecord() {
    if (operationPending.current || isStateUnconfirmed) return
    if (state.timeRecordId !== null) {
      operationPending.current = true
      setIsBusy(true)
      try {
        const record = await timeRecords.endTimeRecord(state.timeRecordId)
        pendingLoads.current.forEach(pending => pending.push(record))
        dispatch({ type: 'recordEnded', record })
      } catch (error) {
        try {
          await getTimeRecords()
        } catch (reloadError) {
          setIsStateUnconfirmed(true)
          throw reloadError
        }
        throw error
      } finally {
        operationPending.current = false
        setIsBusy(false)
      }
    }
  }

  return { state, isBusy, isStateUnconfirmed, startTimeRecord, endTimeRecord, reloadTimeRecords }
}
