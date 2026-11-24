import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import type { ComponentProps } from 'react'
import { beforeEach, describe, it, vi, type Mock } from 'vitest'
import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'
import type { TimeRecorderControls } from '../../../../src/frontends/react/src/presentation/timeRecorderOperationState'
import { should } from '../../../should.mjs'
import { createTimeRecordServiceStub } from '../test/createTimeRecordServiceStub'
import { presentedRecord, timestamp } from '../test/timeRecords'
import TimeRecorder, { TimeRecorderView } from '../../../../src/frontends/react/src/timeRecords/TimeRecorder'
import { initialTimeRecorderState } from '../../../../src/frontends/react/src/presentation/timeRecorderState'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 記録 = presentedRecord(記録ID, 開始時刻)
const 終了済み記録 = presentedRecord(記録ID, 開始時刻, 終了時刻)

describe('TimeRecorderView', () => {
  let view: ComponentProps<typeof TimeRecorderView>
  const 未確認時の操作: TimeRecorderControls = { canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' }

  beforeEach(() => {
    view = {
      state: initialTimeRecorderState,
      controls: { canChangeRecord: true, canReload: true, notice: null },
      startTimeRecord: vi.fn(),
      endTimeRecord: vi.fn(),
      reloadTimeRecords: vi.fn(),
    }
  })

  it('記録操作が許可されていない間は開始ボタンが無効になり、許可されると有効になります', () => {
    const { rerender } = render(<TimeRecorderView {...view} controls={{ ...view.controls, canChangeRecord: false }} />)
    screen.getByRole('button', { name: '記録を開始' }).should.toBeDisabled()

    rerender(<TimeRecorderView {...view} />)

    screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled()
  })

  describe('一覧に終了済みの記録が1件あるとき', () => {
    beforeEach(() => {
      render(<TimeRecorderView {...view} state={{ ...view.state, records: [終了済み記録], displayedRecord: 終了済み記録 }} />)
    })

    it('終了済みの記録が一件あると、画面に「1件」と表示されます', () => {
      screen.getByText('1件').should.toBeInTheDocument()
    })

    it('終了済みの記録の作業時間を操作欄と作業記録一覧に表示します', () => {
      const panel = within(screen.getByRole('region', { name: 'プロジェクト未指定' }))
      const list = within(screen.getByRole('list', { name: '作業記録一覧' }))

      panel.getByText('作業時間').should.toBeInTheDocument()
      panel.getByText('1時間30分').should.toBeInTheDocument()
      list.getByText('作業時間').should.toBeInTheDocument()
      list.getByText('1時間30分').should.toBeInTheDocument()
    })

    it('一覧の記録が終了済みで終了対象がなければ、操作欄に「待機中」と開始ボタンを表示します', () => {
      screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
      should.not.exist(screen.queryByRole('button', { name: '記録を終了' }))
      screen.getByText('待機中').should.toBeInTheDocument()
    })
  })

  describe('一覧の記録が記録中で終了対象になっているとき', () => {
    beforeEach(() => {
      render(<TimeRecorderView {...view} state={{ ...view.state, records: [記録], timeRecordId: 記録ID }} />)
    })

    it('記録中の操作パネルには「記録中」と終了ボタンが表示され、開始ボタンは表示されません', () => {
      screen.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
      should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
      screen.getByText('記録中', { selector: '.status-badge' }).should.toBeInTheDocument()
    })

    it('作業記録一覧には、進行中の記録を「記録中」として表示します', () => {
      const list = within(screen.getByRole('list', { name: '作業記録一覧' }))
      list.getByText('記録中').should.toBeInTheDocument()
    })
  })

  it('操作欄の表示が終了済みの記録から新しい記録に変わると、前回の終了時刻が消えます', () => {
    const { rerender } = render(
      <TimeRecorderView {...view} state={{ ...view.state, records: [終了済み記録], displayedRecord: 終了済み記録 }} />,
    )
    const panel = within(screen.getByRole('region', { name: 'プロジェクト未指定' }))
    panel.getByLabelText('終了時刻').should.toBeInTheDocument()
    const 次の記録 = presentedRecord(次の記録ID, 終了時刻)

    rerender(
      <TimeRecorderView
        {...view}
        state={{ ...view.state, records: [終了済み記録, 次の記録], timeRecordId: 次の記録ID, displayedRecord: 次の記録 }}
      />,
    )

    panel.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
    panel.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 19:30')
    should.not.exist(panel.queryByLabelText('終了時刻'))
  })

  it.each([
    { timeRecordId: null, operationName: '記録を開始' },
    { timeRecordId: 記録ID, operationName: '記録を終了' },
  ])('記録一覧の最新状態が未確認の間は「$operationName」ボタンを押せません', ({ timeRecordId, operationName }) => {
    render(<TimeRecorderView {...view} state={{ ...view.state, timeRecordId }} controls={未確認時の操作} />)

    screen.getByRole('button', { name: operationName }).should.toBeDisabled()
  })

  it('記録一覧の最新状態が未確認のときは、案内を表示し、再取得の通信中だけ再取得ボタンを無効にします', () => {
    const { rerender } = render(<TimeRecorderView {...view} controls={未確認時の操作} />)

    screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')
    screen.getByRole('button', { name: '再取得' }).should.toBeEnabled()

    rerender(<TimeRecorderView {...view} controls={{ ...未確認時の操作, canReload: false }} />)

    screen.getByRole('button', { name: '再取得' }).should.toBeDisabled()
  })
})

describe('TimeRecorder', () => {
  let getTimeRecords: Mock<TimeRecordService['getTimeRecords']>
  let startTimeRecord: Mock<TimeRecordService['startTimeRecord']>
  let endTimeRecord: Mock<TimeRecordService['endTimeRecord']>
  let timeRecords: TimeRecordService

  beforeEach(() => {
    getTimeRecords = vi.fn<TimeRecordService['getTimeRecords']>().mockResolvedValue([])
    startTimeRecord = vi.fn<TimeRecordService['startTimeRecord']>().mockResolvedValue(記録)
    endTimeRecord = vi.fn<TimeRecordService['endTimeRecord']>().mockResolvedValue(終了済み記録)
    timeRecords = createTimeRecordServiceStub({ getTimeRecords, startTimeRecord, endTimeRecord })
  })

  it('記録の開始に失敗すると、記録画面に失敗を知らせるメッセージが表示されます', async () => {
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const alert = await screen.findByRole('alert')
    alert.should.toHaveTextContent('失敗')
  })

  it('開始ボタンで開始要求を送り、応答の記録の開始時刻と終了ボタンを表示します', async () => {
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const endButton = await screen.findByRole('button', { name: '記録を終了' })
    endButton.should.toBeInTheDocument()
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
  })

  it('終了ボタンで記録中の作業の終了を要求し、終了済みの記録を一覧に表示します', async () => {
    getTimeRecords.mockResolvedValue([記録])
    render(<TimeRecorder timeRecords={timeRecords} />)
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
    render(<TimeRecorder timeRecords={timeRecords} />)
    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    const retry = await screen.findByRole('button', { name: '再取得' })
    await waitFor(() => retry.should.toBeEnabled())

    return retry
  }
})
