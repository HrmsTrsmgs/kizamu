import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { createTimeRecordApiMock } from '../test/createTimeRecordApiMock'
import TimeRecorder from './TimeRecorder'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 次の開始時刻 = '2026-08-01T11:00:00Z'

describe('TimeRecorder', () => {
  it('画面を表示すると作業記録一覧を要求します', () => {
    const getTimeRecords = vi.fn(async () => [])
    const timeRecords = createTimeRecordApiMock({
      getTimeRecords,
    })

    render(<TimeRecorder timeRecords={timeRecords} />)

    expect(getTimeRecords).toHaveBeenCalledOnce()
  })

  it('開始ボタンを押すと記録開始を要求します', () => {
    const startTimeRecord = vi.fn(async () => ({
      id: 記録ID,
      startedAt: 開始時刻,
    }))
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord,
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    expect(startTimeRecord).toHaveBeenCalledOnce()
  })

  it('記録を開始すると開始時刻が表示されます', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const startedAtDisplay = await screen.findByLabelText('開始時刻')
    expect(startedAtDisplay).toHaveAttribute('datetime', 開始時刻)
    expect(startedAtDisplay).not.toBeEmptyDOMElement()
  })

  it('記録を開始すると終了操作へ切り替わります', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    expect(await screen.findByRole('button', { name: '記録を終了' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '記録を開始' })).not.toBeInTheDocument()
  })

  it('終了ボタンを押すと開始した記録の終了を要求します', async () => {
    const endTimeRecord = vi.fn(async () => ({ id: 記録ID, startedAt: '', endedAt: '' }))
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
      }),
      endTimeRecord,
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を終了' }))

    expect(endTimeRecord).toHaveBeenCalledOnce()
    expect(endTimeRecord).toHaveBeenCalledWith(記録ID)
  })

  it('記録を終了すると終了時刻が表示されます', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
      }),
      endTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: 終了時刻,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を終了' }))

    const endedAtDisplay = await screen.findByLabelText('終了時刻')
    expect(endedAtDisplay).toHaveAttribute('datetime', 終了時刻)
    expect(endedAtDisplay).not.toBeEmptyDOMElement()
  })

  it('記録を終了すると開始操作へ切り替わります', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
      }),
      endTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: 終了時刻,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を終了' }))

    expect(await screen.findByRole('button', { name: '記録を開始' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '記録を終了' })).not.toBeInTheDocument()
  })

  it('記録を再度開始すると前回の終了時刻が表示されなくなります', async () => {
    const startTimeRecord = vi.fn()
      .mockResolvedValueOnce({
        id: 記録ID,
        startedAt: 開始時刻,
      })
      .mockResolvedValueOnce({
        id: 次の記録ID,
        startedAt: 次の開始時刻,
      })
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord,
      endTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: 終了時刻,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を終了' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を開始' }))
    await screen.findByRole('button', { name: '記録を終了' })

    expect(screen.queryByLabelText('終了時刻')).not.toBeInTheDocument()
  })
})
