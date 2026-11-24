import type { ReactNode } from 'react'
import type { TimeRecorderControls } from '../presentation/timeRecorderOperationState'
import type { TimeRecorderState } from '../presentation/timeRecorderState'
import TimeRecordTimes from './TimeRecordTimes'

type TimeRecorderViewProps = {
  state: TimeRecorderState
  controls: TimeRecorderControls
  startTimeRecord: () => void
  endTimeRecord: () => void
  reloadTimeRecords: () => void
}

export function TimeRecorderView(props: TimeRecorderViewProps) {
  return (
    <div className="time-recorder">
      <RecorderPanel {...props} />
      <TimeRecordList records={props.state.records} />
    </div>
  )
}

type RecorderPanelProps = Omit<TimeRecorderViewProps, 'state'> & {
  state: Pick<TimeRecorderState, 'selectedProject' | 'timeRecordId' | 'displayedRecord'>
}

function RecorderPanel({ state, controls, startTimeRecord, endTimeRecord, reloadTimeRecords }: RecorderPanelProps) {
  const isRecording = state.timeRecordId !== null
  let notice: ReactNode
  switch (controls.notice) {
    case 'stateUnconfirmed':
      notice = (
        <>
          <p role="alert">最新状態を確認できません。再取得してください。</p>
          <button type="button" disabled={!controls.canReload} onClick={reloadTimeRecords}>再取得</button>
        </>
      )
      break
    case 'operationFailed':
      notice = <p role="alert">記録操作に失敗しました。</p>
      break
    default:
      notice = null
  }

  return (
    <section className="recorder-panel" aria-labelledby="recorder-title">
      <header className="panel-header">
        <h2 id="recorder-title">{state.selectedProject?.name ?? 'プロジェクト未指定'}</h2>
        <span className={`status-badge ${isRecording ? 'is-active' : ''}`}>
          {isRecording ? '記録中' : '待機中'}
        </span>
      </header>

      <div className="recorder-action">
        <button
          className={isRecording ? 'record-button is-active' : 'record-button'}
          type="button"
          disabled={!controls.canChangeRecord}
          onClick={isRecording ? endTimeRecord : startTimeRecord}
        >
          {isRecording ? '記録を終了' : '記録を開始'}
        </button>
        {notice}
        {state.displayedRecord !== null && (
          <TimeRecordTimes record={state.displayedRecord.record} labelled />
        )}
      </div>
    </section>
  )
}

function TimeRecordList({ records }: Pick<TimeRecorderState, 'records'>) {
  return (
    <section className="records-panel" aria-labelledby="records-title">
      <header className="panel-header records-header">
        <h2 id="records-title">作業記録</h2>
        <span className="record-count">{records.length}件</span>
      </header>

      <ul className="records-list" aria-label="作業記録一覧">
        {records.map(record => (
          <li key={record.record.id}>
            <span className={`record-status ${record.record.endedAt === null ? 'is-active' : ''}`} aria-hidden="true" />
            <TimeRecordTimes record={record.record} />
          </li>
        ))}
      </ul>
    </section>
  )
}
