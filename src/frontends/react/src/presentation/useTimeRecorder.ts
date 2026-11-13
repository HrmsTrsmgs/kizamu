import { useCallback, useEffect, useReducer, useRef } from 'react'
import type { Project } from 'kizamu-domain-typescript'
import type { TimeRecordService } from './timeRecordService'
import type { TimeRecordPresentation } from './timeRecordPresentation'
import { PendingTimeRecordLoads } from './pendingTimeRecordLoads'
import { initialTimeRecorderState, timeRecorderReducer, type TimeRecorderAction } from './timeRecorderState'
import { getTimeRecorderControls, initialTimeRecorderOperationState, timeRecorderOperationReducer } from './timeRecorderOperationState'

export function useTimeRecorder(timeRecords: TimeRecordService) {
  const [state, dispatch] = useReducer(timeRecorderReducer, initialTimeRecorderState)
  const [operationState, dispatchOperation] = useReducer(timeRecorderOperationReducer, initialTimeRecorderOperationState)
  const operationPending = useRef(false)
  const pendingLoads = useRef(new PendingTimeRecordLoads())

  const getTimeRecords = useCallback(async () => {
    const load = pendingLoads.current.start()
    try {
      const records = await timeRecords.getTimeRecords()
      dispatch({ type: 'recordsLoaded', records, updatedRecords: load.updatedRecords })
      dispatchOperation('recordsConfirmed')
    } finally {
      load.finish()
    }
  }, [timeRecords])

  useEffect(() => {
    void getTimeRecords()
  }, [getTimeRecords])

  function runOperation(operation: () => Promise<void>) {
    if (operationPending.current) return Promise.resolve()
    operationPending.current = true
    dispatchOperation('operationStarted')

    const completion = operation()
    // Observe failures for the view while returning the original promise to callers.
    void completion.then(
      () => finishOperation('operationSucceeded'),
      () => finishOperation('operationFailed'),
    )
    return completion
  }

  function finishOperation(outcome: 'operationSucceeded' | 'operationFailed') {
    operationPending.current = false
    dispatchOperation(outcome)
  }

  async function reloadAfterOperationFailure() {
    try {
      await getTimeRecords()
    } catch (error) {
      dispatchOperation('recordsUnconfirmed')
      throw error
    }
  }

  function changeTimeRecord(
    request: () => Promise<TimeRecordPresentation>,
    toAction: (record: TimeRecordPresentation) => TimeRecorderAction,
  ) {
    if (operationState.isStateUnconfirmed) return Promise.resolve()
    return runOperation(async () => {
      try {
        const record = await request()
        pendingLoads.current.recordUpdate(record)
        dispatch(toAction(record))
      } catch (error) {
        await reloadAfterOperationFailure()
        throw error
      }
    })
  }

  function startTimeRecord() {
    const project = state.selectedProject
    return changeTimeRecord(
      () => timeRecords.startTimeRecord(),
      record => ({ type: 'recordStarted', record, project }),
    )
  }

  function endTimeRecord() {
    const id = state.timeRecordId
    if (id === null) return Promise.resolve()
    return changeTimeRecord(
      () => timeRecords.endTimeRecord(id),
      record => ({ type: 'recordEnded', record }),
    )
  }

  const selectProject = useCallback((project: Project | undefined) => {
    dispatch({ type: 'projectSelected', project })
  }, [])

  return {
    state,
    selectProject,
    isBusy: operationState.isBusy,
    isStateUnconfirmed: operationState.isStateUnconfirmed,
    controls: getTimeRecorderControls(operationState),
    startTimeRecord,
    endTimeRecord,
    reloadTimeRecords: () => runOperation(getTimeRecords),
  }
}
