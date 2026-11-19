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

  it('記録操作が許可されていない間は開始ボタンが無効になり、許可されると有効になります', () => {
    const { rerender } = render(<TimeRecorderView {...view} controls={{ ...view.controls, canChangeRecord: false }} />)
    screen.getByRole('button', { name: '記録を開始' }).should.toBeDisabled()

    rerender(<TimeRecorderView {...view} />)

    screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled()
  })

  it('開始ボタンを一度押すと、記録の開始だけを一度要求します', () => {
    render(<TimeRecorderView {...view} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
  })

  it('終了ボタンを一度押すと、記録の終了だけを一度要求します', () => {
    render(<TimeRecorderView {...view} state={{ ...view.state, timeRecordId: 記録ID }} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を終了' }))

    endTimeRecord.should.have.callCount(1)
    startTimeRecord.should.have.callCount(0)
  })

  describe('一覧に終了済みの記録が1件あるとき', () => {
    beforeEach(() => {
      render(<TimeRecorderView {...view} state={{ ...view.state, records: [終了済み記録] }} />)
    })

    it('終了済みの記録が一件あると、画面に「1件」と表示されます', () => {
      screen.getByText('1件').should.toBeInTheDocument()
    })

    it('作業記録一覧には、終了済みの記録の開始・終了時刻が日本時間で表示されます', () => {
      const list = within(screen.getByRole('list', { name: '作業記録一覧' }))

      list.getByText('2026/08/01 18:00').should.toHaveAttribute('datetime', '2026-08-01T09:00:00.000Z')
      list.getByText('2026/08/01 19:30').should.toHaveAttribute('datetime', '2026-08-01T10:30:00.000Z')
    })

    it('作業記録一覧には、終了済みの記録の開始から終了までが「作業時間」として表示されます', () => {
      const list = within(screen.getByRole('list', { name: '作業記録一覧' }))

      list.getByText('作業時間').should.toBeInTheDocument()
      list.getByText('1時間30分').should.toBeInTheDocument()
    })

    it('一覧の記録が終了済みで終了対象がなければ、操作パネルに開始ボタンが表示されます', () => {
      screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
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

    it('作業記録一覧には、進行中の記録の開始時刻と「記録中」が表示されます', () => {
      const list = within(screen.getByRole('list', { name: '作業記録一覧' }))
      list.getByText('2026/08/01 18:00').should.toBeInTheDocument()
      list.getByText('記録中').should.toBeInTheDocument()
    })
  })

  describe.each([
    { timeRecordId: null, operationName: '記録を開始' },
    { timeRecordId: 記録ID, operationName: '記録を終了' },
  ])('最新状態が未確認で「$operationName」を表示するとき', ({ timeRecordId, operationName }) => {
    beforeEach(() => {
      render(<TimeRecorderView {...view} state={{ ...view.state, timeRecordId }} controls={未確認時の操作} />)
    })

    it('記録一覧の最新状態が未確認の間は、表示中の記録操作ボタンを押せません', () => {
      screen.getByRole('button', { name: operationName }).should.toBeDisabled()
    })

    it('記録一覧の最新状態が未確認のときは、その案内が表示され、再取得ボタンを押せます', () => {
      screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')
      screen.getByRole('button', { name: '再取得' }).should.toBeEnabled()
    })
  })

  it('未確認の案内で再取得ボタンを押すと、記録一覧の再取得だけを要求します', () => {
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

  it('記録の開始に失敗すると、記録画面に失敗を知らせるメッセージが表示されます', async () => {
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const alert = await screen.findByRole('alert')
    alert.should.toHaveTextContent('失敗')
  })

  it('開始ボタンで記録を開始すると、終了ボタンとサーバーが返した開始時刻が表示されます', async () => {
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    ;(await screen.findByRole('button', { name: '記録を終了' })).should.toBeInTheDocument()
    startTimeRecord.should.have.callCount(1)
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
    screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', '2026-08-01T09:00:00.000Z')
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
    screen.getByRole('listitem').should.toHaveTextContent('2026/08/01 18:00')
  })

  describe('記録を開始した画面', () => {
    let endButton: HTMLElement

    beforeEach(async () => {
      render(<TimeRecorder timeRecords={timeRecords} />)
      fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
      endButton = await screen.findByRole('button', { name: '記録を終了' })
    })

    it('終了ボタンで記録を終了したら待機中に戻り確定した終了時刻と作業時間を表示します', async () => {
      fireEvent.click(endButton)

      const startButton = await screen.findByRole('button', { name: '記録を開始' })
      startButton.should.toBeInTheDocument()
      endTimeRecord.should.have.been.calledOnceWith(記録ID)
      should.not.exist(screen.queryByRole('button', { name: '記録を終了' }))
      screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', '2026-08-01T10:30:00.000Z')
      screen.getByLabelText('終了時刻').should.toHaveTextContent('2026/08/01 19:30')
      screen.getByText('待機中').should.toBeInTheDocument()
      screen.getAllByText('1時間30分').should.have.lengthOf(2)
    })

    it('記録を終了してから新しい記録を開始すると、操作欄から前回の終了時刻が消えます', async () => {
      fireEvent.click(endButton)
      await screen.findByLabelText('終了時刻')
      startTimeRecord.mockResolvedValueOnce(presentedRecord(次の記録ID, 終了時刻))

      fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
      await screen.findByRole('button', { name: '記録を終了' })

      should.not.exist(screen.queryByLabelText('終了時刻'))
    })
  })

  describe('一覧を確認できず再取得を待つ画面', () => {
    let retry: HTMLElement

    beforeEach(async () => {
      getTimeRecords.mockResolvedValueOnce([]).mockRejectedValueOnce(new Error('一覧取得失敗'))
      startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
      render(<TimeRecorder timeRecords={timeRecords} />)
      fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

      retry = await screen.findByRole('button', { name: '再取得' })
      await waitFor(() => retry.should.toBeEnabled())
    })

    it('再取得に成功すると未確認の案内から記録操作の失敗表示に戻ります', async () => {
      screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')

      fireEvent.click(retry)

      await screen.findByText('記録操作に失敗しました。')
      should.not.exist(screen.queryByRole('button', { name: '再取得' }))
    })

    it('再取得で空の一覧を確認すると記録を開始できるようになります', async () => {
      fireEvent.click(retry)

      await waitFor(() => screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled())
    })

    it('一覧の再取得に失敗しても、未確認の案内が残り、再取得ボタンをもう一度押せます', async () => {
      getTimeRecords.mockRejectedValueOnce(new Error('再取得失敗'))

      fireEvent.click(retry)

      await waitFor(() => retry.should.toBeEnabled())
      screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')
      getTimeRecords.should.have.callCount(3)
    })
  })
})
