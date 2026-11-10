import type { TimeRecordService } from '../presentation/timeRecordService'
import { useTimeRecorder } from '../presentation/useTimeRecorder'
import { getTimeRecordTimestamps } from '../presentation/timeRecordPresentation'
import TimeRecordTimes from './TimeRecordTimes'

type TimeRecorderProps = {
  timeRecords: TimeRecordService
}

export default function TimeRecorder({ timeRecords }: TimeRecorderProps) {
  const { state, startTimeRecord, endTimeRecord } = useTimeRecorder(timeRecords)

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
