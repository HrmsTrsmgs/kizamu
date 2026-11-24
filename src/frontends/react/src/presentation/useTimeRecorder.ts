import { useCallback, useLayoutEffect, useReducer, useRef } from 'react'
import type { Project } from 'kizamu-domain-typescript'
import type { TimeRecordService } from './timeRecordService'
import type { TimeRecordPresentation } from './timeRecordPresentation'
import { PendingTimeRecordLoads } from './pendingTimeRecordLoads'
import { initialTimeRecorderState, timeRecorderReducer, type TimeRecorderAction } from './timeRecorderState'
import { getTimeRecorderControls, initialTimeRecorderOperationState, timeRecorderOperationReducer } from './timeRecorderOperationState'

type RecorderRequests = {
  operationPending: boolean
  pendingLoads: PendingTimeRecordLoads
  loadVersion: number
}

export function useTimeRecorder(timeRecords: TimeRecordService | undefined) {
  const [state, dispatch] = useReducer(timeRecorderReducer, initialTimeRecorderState)
  const [operationState, dispatchOperation] = useReducer(timeRecorderOperationReducer, initialTimeRecorderOperationState)
  const activeRequests = useRef<RecorderRequests | undefined>(undefined)

  const getTimeRecords = useCallback(async () => {
    const requests = activeRequests.current
    if (!timeRecords || !requests) return
    const version = requests.loadVersion
    const load = requests.pendingLoads.start()
    try {
      const records = await timeRecords.getTimeRecords()
      if (requests !== activeRequests.current || version !== requests.loadVersion) return
      dispatch({ type: 'recordsLoaded', records, updatedRecords: load.updatedRecords })
      dispatchOperation('recordsConfirmed')
    } finally {
      load.finish()
    }
  }, [timeRecords])

  // Open the request session before the newly visible controls can be used.
  useLayoutEffect(() => {
    if (timeRecords) {
      activeRequests.current ??= {
        operationPending: false,
        pendingLoads: new PendingTimeRecordLoads(),
        loadVersion: 0,
      }
      void getTimeRecords()
    } else if (activeRequests.current) {
      // Keep the project selection across disconnection and discard the old requests.
      activeRequests.current = undefined
      dispatch({ type: 'sessionEnded' })
      dispatchOperation('sessionEnded')
    }
  }, [timeRecords, getTimeRecords])

  function runOperation(operation: (requests: RecorderRequests) => Promise<void>) {
    const requests = activeRequests.current
    if (!requests || requests.operationPending) return Promise.resolve()
    requests.operationPending = true
    dispatchOperation('operationStarted')

    const completion = operation(requests)
    // Observe failures for the view while returning the original promise to callers.
    void completion.then(
      () => finishOperation(requests, 'operationSucceeded'),
      () => finishOperation(requests, 'operationFailed'),
    )
    return completion
  }

  function finishOperation(requests: RecorderRequests, outcome: 'operationSucceeded' | 'operationFailed') {
    if (requests !== activeRequests.current) return
    requests.operationPending = false
    dispatchOperation(outcome)
  }

  async function reloadAfterOperationFailure(requests: RecorderRequests) {
    try {
      await getTimeRecords()
    } catch (error) {
      if (requests === activeRequests.current) dispatchOperation('recordsUnconfirmed')
      throw error
    }
  }

  function changeTimeRecord(
    request: () => Promise<TimeRecordPresentation>,
    toAction: (record: TimeRecordPresentation) => TimeRecorderAction,
  ) {
    if (operationState.isStateUnconfirmed) return Promise.resolve()
    return runOperation(async requests => {
      try {
        const record = await request()
        if (requests !== activeRequests.current) return
        requests.pendingLoads.recordUpdate(record)
        dispatch(toAction(record))
      } catch (error) {
        if (requests === activeRequests.current) {
          requests.loadVersion++
          await reloadAfterOperationFailure(requests)
        }
        throw error
      }
    })
  }

  function startTimeRecord() {
    if (!timeRecords || state.timeRecordId !== null) return Promise.resolve()
    const project = state.selectedProject
    return changeTimeRecord(
      () => timeRecords.startTimeRecord(),
      record => ({ type: 'recordStarted', record, project }),
    )
  }

  function endTimeRecord() {
    const id = state.timeRecordId
    if (!timeRecords || id === null) return Promise.resolve()
    const project = state.selectedProject
    return changeTimeRecord(
      () => timeRecords.endTimeRecord(id),
      record => ({ type: 'recordEnded', record, project }),
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
