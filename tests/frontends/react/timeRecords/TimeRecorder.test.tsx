import { fireEvent, render, screen, within } from '@testing-library/react'
import { beforeEach, describe, it, vi, type Mock } from 'vitest'
import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'
import { should } from '../../../should.mjs'
import { createTimeRecordServiceStub } from '../test/createTimeRecordServiceStub'
import { presentedRecord, timestamp } from '../test/timeRecords'
import TimeRecorder from '../../../../src/frontends/react/src/timeRecords/TimeRecorder'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 記録 = presentedRecord(記録ID, 開始時刻)
const 終了済み記録 = presentedRecord(記録ID, 開始時刻, 終了時刻)

describe('TimeRecorder', () => {
  let getTimeRecords: Mock<TimeRecordService['getTimeRecords']>
  let startTimeRecord: Mock<TimeRecordService['startTimeRecord']>
  let endTimeRecord: Mock<TimeRecordService['endTimeRecord']>
  let timeRecords: TimeRecordService

  beforeEach(() => {
    getTimeRecords = vi.fn<TimeRecordService['getTimeRecords']>().mockResolvedValue([])
    startTimeRecord = vi.fn<TimeRecordService['startTimeRecord']>()
    endTimeRecord = vi.fn<TimeRecordService['endTimeRecord']>()
    timeRecords = createTimeRecordServiceStub({ getTimeRecords, startTimeRecord, endTimeRecord })
  })

  it('取得した終了済み記録の日本時間と作業時間を一覧に表示します', async () => {
    getTimeRecords.mockResolvedValue([終了済み記録])

    render(<TimeRecorder timeRecords={timeRecords} />)

    const recordCount = await screen.findByText('1件')
    recordCount.should.toBeInTheDocument()
    const list = screen.getByRole('list', { name: '作業記録一覧' })
    within(list).getByText('2026/08/01 18:00').should.toHaveAttribute('datetime', 開始時刻.isoText)
    within(list).getByText('2026/08/01 19:30').should.toHaveAttribute('datetime', 終了時刻.isoText)
    within(list).getByText('作業時間').should.toBeInTheDocument()
    within(list).getByText('1時間30分').should.toBeInTheDocument()
    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
  })

  it('取得した記録中の記録を一覧に表示して終了操作を接続します', async () => {
    getTimeRecords.mockResolvedValue([記録])
    endTimeRecord.mockResolvedValue(終了済み記録)

    render(<TimeRecorder timeRecords={timeRecords} />)

    const endButton = await screen.findByRole('button', { name: '記録を終了' })
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
    screen.getByText('記録中', { selector: '.status-badge' }).should.toBeInTheDocument()
    const list = screen.getByRole('list', { name: '作業記録一覧' })
    within(list).getByText('2026/08/01 18:00').should.toBeInTheDocument()
    within(list).getByText('記録中').should.toBeInTheDocument()

    fireEvent.click(endButton)

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    const startButton = await screen.findByRole('button', { name: '記録を開始' })
    startButton.should.toBeInTheDocument()
  })

  it('開始と終了の操作を画面状態と時刻表示へ接続します', async () => {
    startTimeRecord.mockResolvedValue(記録)
    endTimeRecord.mockResolvedValue(終了済み記録)
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
