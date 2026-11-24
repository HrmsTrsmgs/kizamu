import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, it, vi, type Mock } from 'vitest'
import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'
import { should } from '../../../should.mjs'
import { presentedRecord, timestamp } from '../test/timeRecords'
import AuthenticatedTimeRecords from '../../../../src/frontends/react/src/timeRecords/AuthenticatedTimeRecords'
import { TimeRecorderView } from '../../../../src/frontends/react/src/timeRecords/TimeRecorder'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 記録 = presentedRecord(記録ID, 開始時刻)
const 終了済み記録 = presentedRecord(記録ID, 開始時刻, 終了時刻)

it('終了済みの記録の作業時間を操作欄と作業記録一覧に表示します', () => {
  render(
    <TimeRecorderView
      records={[終了済み記録.record]}
      displayedRecord={終了済み記録.record}
      controls={{ canChangeRecord: true, canReload: true, notice: null }}
      isRecording={false}
      toggleRecording={vi.fn()}
      reloadTimeRecords={vi.fn()}
    />,
  )

  const panel = within(screen.getByRole('region', { name: 'プロジェクト未指定' }))
  const list = within(screen.getByRole('list', { name: '作業記録一覧' }))
  panel.getByText('作業時間').should.toBeInTheDocument()
  panel.getByText('1時間30分').should.toBeInTheDocument()
  list.getByText('作業時間').should.toBeInTheDocument()
  list.getByText('1時間30分').should.toBeInTheDocument()
})

describe('AuthenticatedTimeRecordsの記録操作', () => {
  let getTimeRecords: Mock<TimeRecordService['getTimeRecords']>
  let startTimeRecord: Mock<TimeRecordService['startTimeRecord']>
  let endTimeRecord: Mock<TimeRecordService['endTimeRecord']>
  let timeRecords: TimeRecordService

  beforeEach(() => {
    getTimeRecords = vi.fn<TimeRecordService['getTimeRecords']>().mockResolvedValue([])
    startTimeRecord = vi.fn<TimeRecordService['startTimeRecord']>().mockResolvedValue(記録)
    endTimeRecord = vi.fn<TimeRecordService['endTimeRecord']>().mockResolvedValue(終了済み記録)
    timeRecords = { getTimeRecords, startTimeRecord, endTimeRecord }
  })

  it('記録の開始に失敗すると、記録画面に失敗を知らせるメッセージが表示されます', async () => {
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    render(<AuthenticatedTimeRecords timeRecords={timeRecords} onSignOut={() => {}} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const alert = await screen.findByRole('alert')
    alert.should.toHaveTextContent('失敗')
  })

  it('開始ボタンで開始要求を送り、応答の記録の開始時刻と終了ボタンを表示します', async () => {
    render(<AuthenticatedTimeRecords timeRecords={timeRecords} onSignOut={() => {}} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const endButton = await screen.findByRole('button', { name: '記録を終了' })
    endButton.should.toBeInTheDocument()
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
  })

  it('終了ボタンで記録中の作業の終了を要求し、終了済みの記録を一覧に表示します', async () => {
    getTimeRecords.mockResolvedValue([記録])
    render(<AuthenticatedTimeRecords timeRecords={timeRecords} onSignOut={() => {}} />)
    const endButton = await screen.findByRole('button', { name: '記録を終了' })

    fireEvent.click(endButton)

    const startButton = await screen.findByRole('button', { name: '記録を開始' })
    startButton.should.toBeInTheDocument()
    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    startTimeRecord.should.have.callCount(0)
    const list = within(screen.getByRole('list', { name: '作業記録一覧' }))
    list.getByText('2026/08/01 19:30').should.toBeInTheDocument()
    should.not.exist(list.queryByText('記録中'))
  })

  it('再取得ボタンで一覧を再取得できると未確認の案内から記録操作の失敗表示に戻ります', async () => {
    getTimeRecords.mockResolvedValueOnce([]).mockRejectedValueOnce(new Error('一覧取得失敗'))
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    const retry = await 開始失敗後に再取得できるまで待つ()
    screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')

    fireEvent.click(retry)

    const failure = await screen.findByText('記録操作に失敗しました。')
    failure.should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '再取得' }))
    getTimeRecords.should.have.callCount(3)
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
  })

  async function 開始失敗後に再取得できるまで待つ() {
    render(<AuthenticatedTimeRecords timeRecords={timeRecords} onSignOut={() => {}} />)
    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    const retry = await screen.findByRole('button', { name: '再取得' })
    await waitFor(() => retry.should.toBeEnabled())

    return retry
  }
})
