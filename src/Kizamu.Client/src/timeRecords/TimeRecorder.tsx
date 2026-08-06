import { useEffect, useReducer } from 'react'
import type { TimeRecordApi } from './timeRecordApi'
import { calculateTimeRecordDuration } from './timeRecordDuration'
import TimeRecordTimes from './TimeRecordTimes'
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
    <div className="time-recorder">
      <section className="recorder-panel" aria-labelledby="recorder-title">
        <header className="panel-header">
          <h2 id="recorder-title">プロジェクト未登録</h2>
          <span className={`status-badge ${state.timeRecordId === null ? '' : 'is-active'}`}>
            {state.timeRecordId === null ? '待機中' : '記録中'}
          </span>
        </header>

        <div className="recorder-action">
          {state.timeRecordId === null ? (
            <button className="record-button" type="button" onClick={() => { void startTimeRecord() }}>
              記録を開始
            </button>
          ) : (
            <button className="record-button is-active" type="button" onClick={() => { void endTimeRecord() }}>
              記録を終了
            </button>
          )}
          {state.startedAt !== null && (
            <TimeRecordTimes
              startedAt={state.startedAt}
              endedAt={state.endedAt}
              labelled
            />
          )}
        </div>
      </section>

      <section className="records-panel" aria-labelledby="records-title">
        <header className="panel-header records-header">
          <h2 id="records-title">作業記録</h2>
          <span className="record-count">{state.records.length}件</span>
        </header>

        <ul className="records-list" aria-label="作業記録一覧">
          {state.records.map(record => {
            const duration = record.endedAt === null
              ? null
              : calculateTimeRecordDuration(record.startedAt, record.endedAt)

            return (
              <li key={record.id}>
                <span className={`record-status ${record.endedAt === null ? 'is-active' : ''}`} aria-hidden="true" />
                <TimeRecordTimes startedAt={record.startedAt} endedAt={record.endedAt} />
                {duration !== null && (
                  <span>{duration.hours}時間{duration.minutes}分</span>
                )}
              </li>
            )
          })}
        </ul>
      </section>
    </div>
  )
}
