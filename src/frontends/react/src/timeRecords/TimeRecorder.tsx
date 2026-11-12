import { useEffect } from 'react'
import type { Project } from 'kizamu-domain-typescript'
import type { TimeRecordService } from '../presentation/timeRecordService'
import { useTimeRecorder } from '../presentation/useTimeRecorder'
import type { TimeRecorderControls } from '../presentation/timeRecorderOperationState'
import type { TimeRecorderState } from '../presentation/timeRecorderState'
import { getTimeRecordTimestamps } from '../presentation/timeRecordPresentation'
import TimeRecordTimes from './TimeRecordTimes'

type TimeRecorderProps = {
  timeRecords: TimeRecordService
  selectedProject?: Project
}

export default function TimeRecorder({ timeRecords, selectedProject }: TimeRecorderProps) {
  const { state, selectProject, controls, startTimeRecord, endTimeRecord, reloadTimeRecords } = useTimeRecorder(timeRecords)

  useEffect(() => {
    selectProject(selectedProject)
  }, [selectedProject, selectProject])

  return (
    <TimeRecorderView
      state={state}
      controls={controls}
      startTimeRecord={startTimeRecord}
      endTimeRecord={endTimeRecord}
      reloadTimeRecords={reloadTimeRecords}
    />
  )
}

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
  state: Pick<TimeRecorderState, 'selectedProject' | 'timeRecordId' | 'startedAt' | 'endedAt'>
}

function RecorderPanel({ state, controls, startTimeRecord, endTimeRecord, reloadTimeRecords }: RecorderPanelProps) {
  const isRecording = state.timeRecordId !== null

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
        {controls.notice === 'stateUnconfirmed' && (
          <>
            <p role="alert">最新状態を確認できません。再取得してください。</p>
            <button type="button" disabled={!controls.canReload} onClick={reloadTimeRecords}>再取得</button>
          </>
        )}
        {controls.notice === 'operationFailed' && <p role="alert">記録操作に失敗しました。</p>}
        {state.startedAt !== null && (
          <TimeRecordTimes
            startedAt={state.startedAt}
            endedAt={state.endedAt}
            labelled
          />
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
            <TimeRecordTimes {...getTimeRecordTimestamps(record)} />
          </li>
        ))}
      </ul>
    </section>
  )
}
