import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, it, vi } from 'vitest'
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

describe('TimeRecorder', () => {
  it('通信中はボタンを無効にし完了後に有効にします', () => {
    const startTimeRecord = vi.fn(async () => {})
    const endTimeRecord = vi.fn(async () => {})
    const { rerender } = render(<TimeRecorderView state={initialTimeRecorderState} isBusy={true} startTimeRecord={startTimeRecord} endTimeRecord={endTimeRecord} />)
    screen.getByRole('button', { name: '記録を開始' }).should.toBeDisabled()
    rerender(<TimeRecorderView state={initialTimeRecorderState} isBusy={false} startTimeRecord={startTimeRecord} endTimeRecord={endTimeRecord} />)
    screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled()
  })

  it('開始ボタンを押すと開始操作を呼びます', () => {
    const startTimeRecord = vi.fn(async () => {})
    const endTimeRecord = vi.fn(async () => {})
    render(<TimeRecorderView state={initialTimeRecorderState} isBusy={false} startTimeRecord={startTimeRecord} endTimeRecord={endTimeRecord} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
  })

  it('終了ボタンを押すと終了操作を呼びます', () => {
    const startTimeRecord = vi.fn(async () => {})
    const endTimeRecord = vi.fn(async () => {})
    const state = { ...initialTimeRecorderState, timeRecordId: 記録ID }
    render(<TimeRecorderView state={state} isBusy={false} startTimeRecord={startTimeRecord} endTimeRecord={endTimeRecord} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を終了' }))

    endTimeRecord.should.have.callCount(1)
    startTimeRecord.should.have.callCount(0)
  })

  it('終了済み記録の日本時間と作業時間を一覧に表示します', () => {
    const state = { ...initialTimeRecorderState, records: [終了済み記録] }
    render(<TimeRecorderView state={state} isBusy={false} startTimeRecord={async () => {}} endTimeRecord={async () => {}} />)

    screen.getByText('1件').should.toBeInTheDocument()
    const list = screen.getByRole('list', { name: '作業記録一覧' })
    within(list).getByText('2026/08/01 18:00').should.toHaveAttribute('datetime', 開始時刻.isoText)
    within(list).getByText('2026/08/01 19:30').should.toHaveAttribute('datetime', 終了時刻.isoText)
    within(list).getByText('作業時間').should.toBeInTheDocument()
    within(list).getByText('1時間30分').should.toBeInTheDocument()
    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
  })

  it('記録中の状態を一覧に表示します', () => {
    const state = { ...initialTimeRecorderState, records: [記録], timeRecordId: 記録ID }
    render(<TimeRecorderView state={state} isBusy={false} startTimeRecord={async () => {}} endTimeRecord={async () => {}} />)

    screen.getByRole('button', { name: '記録を終了' })
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
    screen.getByText('記録中', { selector: '.status-badge' }).should.toBeInTheDocument()
    const list = screen.getByRole('list', { name: '作業記録一覧' })
    within(list).getByText('2026/08/01 18:00').should.toBeInTheDocument()
    within(list).getByText('記録中').should.toBeInTheDocument()
  })

  it('開始と終了の操作を画面状態と時刻表示へ接続します', async () => {
    const startTimeRecord = vi.fn(async () => 記録)
    const endTimeRecord = vi.fn(async () => 終了済み記録)
    const timeRecords = createTimeRecordServiceStub({ startTimeRecord, endTimeRecord })
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

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    await screen.findByRole('button', { name: '記録を終了' })

    should.not.exist(screen.queryByLabelText('終了時刻'))
  })
})
