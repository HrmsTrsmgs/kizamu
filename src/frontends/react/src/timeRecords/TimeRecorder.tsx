import { useState } from 'react'
import type { TimeRecordService } from '../presentation/timeRecordService'
import { useTimeRecorder } from '../presentation/useTimeRecorder'
import type { TimeRecorderState } from '../presentation/timeRecorderState'
import { getTimeRecordTimestamps } from '../presentation/timeRecordPresentation'
import TimeRecordTimes from './TimeRecordTimes'

type TimeRecorderProps = {
  timeRecords: TimeRecordService
}

export default function TimeRecorder({ timeRecords }: TimeRecorderProps) {
  const { state, isBusy, startTimeRecord, endTimeRecord } = useTimeRecorder(timeRecords)

  return <TimeRecorderView state={state} isBusy={isBusy} startTimeRecord={startTimeRecord} endTimeRecord={endTimeRecord} />
}

type TimeRecorderViewProps = {
  state: TimeRecorderState
  isBusy: boolean
  startTimeRecord: () => Promise<void>
  endTimeRecord: () => Promise<void>
}

export function TimeRecorderView({ state, isBusy, startTimeRecord, endTimeRecord }: TimeRecorderViewProps) {
  const [operationFailed, setOperationFailed] = useState(false)

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
            <button className="record-button" type="button" disabled={isBusy} onClick={() => { void startTimeRecord().catch(() => setOperationFailed(true)) }}>
              記録を開始
            </button>
          ) : (
            <button className="record-button is-active" type="button" disabled={isBusy} onClick={() => { void endTimeRecord().catch(() => setOperationFailed(true)) }}>
              記録を終了
            </button>
          )}
          {operationFailed && <p role="alert">記録操作に失敗しました。</p>}
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
          {state.records.map(record => (
            <li key={record.record.id}>
              <span className={`record-status ${record.record.endedAt === null ? 'is-active' : ''}`} aria-hidden="true" />
              <TimeRecordTimes {...getTimeRecordTimestamps(record)} />
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
