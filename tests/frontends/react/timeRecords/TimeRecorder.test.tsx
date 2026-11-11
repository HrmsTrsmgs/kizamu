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
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 記録 = presentedRecord(記録ID, 開始時刻)
const 終了済み記録 = presentedRecord(記録ID, 開始時刻, 終了時刻)

describe('TimeRecorderView', () => {
  let startTimeRecord: Mock<() => void>
  let endTimeRecord: Mock<() => void>
  let reloadTimeRecords: Mock<() => void>
  let view: ComponentProps<typeof TimeRecorderView>
  const 未確認時の操作: TimeRecorderControls = { canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' }

  beforeEach(() => {
    startTimeRecord = vi.fn()
    endTimeRecord = vi.fn()
    reloadTimeRecords = vi.fn()
    view = {
      state: initialTimeRecorderState,
      controls: { canChangeRecord: true, canReload: true, notice: null },
      startTimeRecord,
      endTimeRecord,
      reloadTimeRecords,
    }
  })

  it('通信中はボタンを無効にし完了後に有効にします', () => {
    const { rerender } = render(<TimeRecorderView {...view} controls={{ ...view.controls, canChangeRecord: false }} />)
    screen.getByRole('button', { name: '記録を開始' }).should.toBeDisabled()

    rerender(<TimeRecorderView {...view} />)

    screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled()
  })

  it('開始ボタンを押すと開始操作を呼びます', () => {
    render(<TimeRecorderView {...view} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
  })

  it('終了ボタンを押すと終了操作を呼びます', () => {
    render(<TimeRecorderView {...view} state={{ ...view.state, timeRecordId: 記録ID }} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を終了' }))

    endTimeRecord.should.have.callCount(1)
    startTimeRecord.should.have.callCount(0)
  })

  it('終了済み記録の日本時間と作業時間を一覧に表示します', () => {
    render(<TimeRecorderView {...view} state={{ ...view.state, records: [終了済み記録] }} />)

    screen.getByText('1件').should.toBeInTheDocument()
    const list = within(screen.getByRole('list', { name: '作業記録一覧' }))
    list.getByText('2026/08/01 18:00').should.toHaveAttribute('datetime', 開始時刻.isoText)
    list.getByText('2026/08/01 19:30').should.toHaveAttribute('datetime', 終了時刻.isoText)
    list.getByText('作業時間').should.toBeInTheDocument()
    list.getByText('1時間30分').should.toBeInTheDocument()
    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
  })

  it('記録中の状態を一覧に表示します', () => {
    render(<TimeRecorderView {...view} state={{ ...view.state, records: [記録], timeRecordId: 記録ID }} />)

    screen.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
    screen.getByText('記録中', { selector: '.status-badge' }).should.toBeInTheDocument()
    const list = within(screen.getByRole('list', { name: '作業記録一覧' }))
    list.getByText('2026/08/01 18:00').should.toBeInTheDocument()
    list.getByText('記録中').should.toBeInTheDocument()
  })

  it.each([
    { timeRecordId: null, operationName: '記録を開始' },
    { timeRecordId: 記録ID, operationName: '記録を終了' },
  ])('最新状態を確認できないときは$operationNameを無効にして再取得を案内します', ({ timeRecordId, operationName }) => {
    render(<TimeRecorderView {...view} state={{ ...view.state, timeRecordId }} controls={未確認時の操作} />)

    screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')
    screen.getByRole('button', { name: operationName }).should.toBeDisabled()
    screen.getByRole('button', { name: '再取得' }).should.toBeEnabled()
  })

  it('再取得ボタンを押すと一覧再取得を呼びます', () => {
    render(<TimeRecorderView {...view} controls={未確認時の操作} />)

    fireEvent.click(screen.getByRole('button', { name: '再取得' }))

    reloadTimeRecords.should.have.callCount(1)
    startTimeRecord.should.have.callCount(0)
    endTimeRecord.should.have.callCount(0)
  })

  it('再取得の通信中は再取得ボタンを無効にします', () => {
    render(<TimeRecorderView {...view} controls={{ ...未確認時の操作, canReload: false }} />)

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

  it('開始操作に失敗すると利用者に知らせます', async () => {
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const alert = await screen.findByRole('alert')
    alert.should.toHaveTextContent('失敗')
  })

  it('開始と終了の操作を画面状態と時刻表示へ接続します', async () => {
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const endButton = await screen.findByRole('button', { name: '記録を終了' })
    startTimeRecord.should.have.callCount(1)
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
    screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', 開始時刻.isoText)
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
    screen.getByRole('listitem').should.toHaveTextContent('2026/08/01 18:00')

    fireEvent.click(endButton)

    const startButton = await screen.findByRole('button', { name: '記録を開始' })
    startButton.should.toBeInTheDocument()
    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    should.not.exist(screen.queryByRole('button', { name: '記録を終了' }))
    screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', 終了時刻.isoText)
    screen.getByLabelText('終了時刻').should.toHaveTextContent('2026/08/01 19:30')
    screen.getByText('待機中').should.toBeInTheDocument()
    screen.getAllByText('1時間30分').should.have.lengthOf(2)
  })

  it('再開始すると直前の終了時刻を消します', async () => {
    await 開始と終了を操作した画面を用意する()

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    await screen.findByRole('button', { name: '記録を終了' })

    should.not.exist(screen.queryByLabelText('終了時刻'))
  })

  it('再取得に成功すると未確認の案内から記録操作の失敗表示に戻ります', async () => {
    const retry = await 一覧を確認できず再取得を待つ画面を用意する()
    getTimeRecords.mockResolvedValueOnce([])
    screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')

    fireEvent.click(retry)

    await screen.findByText('記録操作に失敗しました。')
    should.not.exist(screen.queryByRole('button', { name: '再取得' }))
    screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled()
  })

  it('再取得に失敗しても未確認の案内と再取得操作を表示します', async () => {
    const retry = await 一覧を確認できず再取得を待つ画面を用意する()
    getTimeRecords.mockRejectedValueOnce(new Error('再取得失敗'))

    fireEvent.click(retry)

    await waitFor(() => retry.should.toBeEnabled())
    screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')
    getTimeRecords.should.have.callCount(3)
  })

  async function 開始と終了を操作した画面を用意する() {
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を終了' }))
    await screen.findByLabelText('終了時刻')
  }

  async function 一覧を確認できず再取得を待つ画面を用意する() {
    getTimeRecords.mockResolvedValueOnce([]).mockRejectedValueOnce(new Error('一覧取得失敗'))
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    render(<TimeRecorder timeRecords={timeRecords} />)
    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const retry = await screen.findByRole('button', { name: '再取得' })
    await waitFor(() => retry.should.toBeEnabled())
    return retry
  }
})
