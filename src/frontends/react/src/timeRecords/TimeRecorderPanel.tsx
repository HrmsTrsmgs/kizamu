import { useId, type ReactNode } from 'react'
import type { TimeRecord } from 'kizamu-domain-typescript'
import type { TimeRecorderControls } from '../presentation/timeRecorderOperationState'
import TimeRecordTimes from './TimeRecordTimes'

type TimeRecorderPanelProps = {
  projectName?: string
  displayedRecord?: TimeRecord
  controls: TimeRecorderControls
  isRecording: boolean
  toggleRecording: () => void
  reloadTimeRecords: () => void
}

export default function TimeRecorderPanel({
  projectName,
  displayedRecord,
  controls,
  isRecording,
  toggleRecording,
  reloadTimeRecords,
}: TimeRecorderPanelProps) {
  const titleId = useId()
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
    <section className="recorder-panel" aria-labelledby={titleId}>
      <header className="panel-header">
        <h2 id={titleId}>{projectName ?? 'プロジェクト未指定'}</h2>
        <span className={`status-badge ${isRecording ? 'is-active' : ''}`}>
          {isRecording ? '記録中' : '待機中'}
        </span>
      </header>

      <div className="recorder-action">
        <button
          className={isRecording ? 'record-button is-active' : 'record-button'}
          type="button"
          disabled={!controls.canChangeRecord}
          onClick={toggleRecording}
        >
          {isRecording ? '記録を終了' : '記録を開始'}
        </button>
        {notice}
        {displayedRecord && (
          <TimeRecordTimes record={displayedRecord} labelled />
        )}
      </div>
    </section>
  )
}
