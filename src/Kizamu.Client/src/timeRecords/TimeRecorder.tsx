import { useEffect, useReducer } from 'react'
import type { TimeRecordApi } from './timeRecordApi'
import { initialTimeRecorderState, timeRecorderReducer } from './timeRecorderState'

type TimeRecorderProps = {
  timeRecords: TimeRecordApi
}

export default function TimeRecorder({ timeRecords }: TimeRecorderProps) {
  const [state, dispatch] = useReducer(timeRecorderReducer, initialTimeRecorderState)

  useEffect(() => {
    async function getTimeRecords() {
      dispatch({
        type: 'recordsLoaded',
        records: await timeRecords.getTimeRecords(),
      })
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

  return (
    <>
      <ul aria-label="作業記録一覧">
        {state.records.map(record => (
          <li key={record.id}>
            <time dateTime={record.startedAt}>{record.startedAt}</time>
            {record.endedAt !== null && (
              <time dateTime={record.endedAt}>{record.endedAt}</time>
            )}
          </li>
        ))}
      </ul>
      {state.timeRecordId === null ? (
        <button type="button" onClick={() => { void startTimeRecord() }}>
          記録を開始
        </button>
      ) : (
        <button type="button" onClick={() => { void endTimeRecord() }}>
          記録を終了
        </button>
      )}
      {state.startedAt !== null && (
        <time aria-label="開始時刻" dateTime={state.startedAt}>
          {state.startedAt}
        </time>
      )}
      {state.endedAt !== null && (
        <time aria-label="終了時刻" dateTime={state.endedAt}>
          {state.endedAt}
        </time>
      )}
    </>
  )
}
