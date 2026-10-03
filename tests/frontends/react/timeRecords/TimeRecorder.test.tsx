import { act, fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { createTimeRecordApiMock } from '../test/createTimeRecordApiMock'
import TimeRecorder from '../../../../src/frontends/react/src/timeRecords/TimeRecorder'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'
const 開始から1時間31分後の1秒前 = '2026-08-01T10:30:59Z'
const 開始から2時間15分後の終了時刻 = '2026-08-01T11:15:00Z'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 次の開始時刻 = '2026-08-01T11:00:00Z'
const 日本時間で表示 = (dateTime: string) => new Intl.DateTimeFormat('ja-JP', {
  timeZone: 'Asia/Tokyo',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
}).format(new Date(dateTime))
const 表示用開始時刻 = 日本時間で表示(開始時刻)
const 表示用終了時刻 = 日本時間で表示(終了時刻)

describe('TimeRecorder', () => {
  it('画面を表示すると作業記録一覧を要求します', () => {
    const getTimeRecords = vi.fn(async () => [])
    const timeRecords = createTimeRecordApiMock({
      getTimeRecords,
    })

    render(<TimeRecorder timeRecords={timeRecords} />)

    expect(getTimeRecords).toHaveBeenCalledOnce()
  })

  it('取得した作業記録の開始時刻を一覧に表示します', async () => {
    const timeRecords = createTimeRecordApiMock({
      getTimeRecords: async () => [{
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: null,
      }],
    })

    render(<TimeRecorder timeRecords={timeRecords} />)

    const list = await screen.findByRole('list', { name: '作業記録一覧' })
    expect(within(list).getByText(表示用開始時刻)).toBeInTheDocument()
  })

  it('取得した終了済み作業記録の終了時刻を一覧に表示します', async () => {
    const timeRecords = createTimeRecordApiMock({
      getTimeRecords: async () => [{
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: 終了時刻,
      }],
    })

    render(<TimeRecorder timeRecords={timeRecords} />)

    const list = await screen.findByRole('list', { name: '作業記録一覧' })
    expect(within(list).getByText(表示用終了時刻)).toBeInTheDocument()
  })

  it.each([
    { startedAt: 開始時刻, endedAt: 終了時刻, expectedDuration: '1時間30分' },
    { startedAt: 開始時刻, endedAt: 開始から2時間15分後の終了時刻, expectedDuration: '2時間15分' },
  ])('取得した終了済み作業記録では開始時刻から終了時刻までを作業時間として一覧に表示します: $expectedDuration', async ({
    startedAt,
    endedAt,
    expectedDuration,
  }) => {
    const timeRecords = createTimeRecordApiMock({
      getTimeRecords: async () => [{
        id: 記録ID,
        startedAt,
        endedAt,
      }],
    })

    render(<TimeRecorder timeRecords={timeRecords} />)

    const list = await screen.findByRole('list', { name: '作業記録一覧' })
    expect(within(list).getByText('作業時間')).toBeInTheDocument()
    expect(within(list).getByText(expectedDuration)).toBeInTheDocument()
  })

  it.each([
    { currentTime: 終了時刻, expectedDuration: '1時間30分' },
    { currentTime: 開始から2時間15分後の終了時刻, expectedDuration: '2時間15分' },
  ])('取得した記録中の作業記録では開始時刻から現在時刻までを作業時間として一覧に表示します: $expectedDuration', async ({
    currentTime,
    expectedDuration,
  }) => {
    vi.useFakeTimers()
    vi.setSystemTime(currentTime)

    try {
      const timeRecords = createTimeRecordApiMock({
        getTimeRecords: async () => [{
          id: 記録ID,
          startedAt: 開始時刻,
          endedAt: null,
        }],
      })

      render(<TimeRecorder timeRecords={timeRecords} />)

      await vi.waitFor(() => {
        const list = screen.getByRole('list', { name: '作業記録一覧' })
        expect(within(list).getByText(expectedDuration)).toBeInTheDocument()
      })
    } finally {
      vi.useRealTimers()
    }
  })

  it('記録中に時間が経過すると作業時間の表示を更新します', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(終了時刻)

    try {
      const timeRecords = createTimeRecordApiMock({
        getTimeRecords: async () => [{
          id: 記録ID,
          startedAt: 開始時刻,
          endedAt: null,
        }],
      })

      render(<TimeRecorder timeRecords={timeRecords} />)

      await vi.waitFor(() => {
        const list = screen.getByRole('list', { name: '作業記録一覧' })
        expect(within(list).getByText('1時間30分')).toBeInTheDocument()
      })

      await act(async () => {
        await vi.advanceTimersByTimeAsync(60_000)
      })

      const list = screen.getByRole('list', { name: '作業記録一覧' })
      expect(within(list).getByText('1時間31分')).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('記録中の作業時間が次の整数分になると表示を更新します', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(開始から1時間31分後の1秒前)

    try {
      const timeRecords = createTimeRecordApiMock({
        getTimeRecords: async () => [{
          id: 記録ID,
          startedAt: 開始時刻,
          endedAt: null,
        }],
      })

      render(<TimeRecorder timeRecords={timeRecords} />)

      await vi.waitFor(() => {
        const list = screen.getByRole('list', { name: '作業記録一覧' })
        expect(within(list).getByText('1時間30分')).toBeInTheDocument()
      })

      await act(async () => {
        await vi.advanceTimersByTimeAsync(1_000)
      })

      const list = screen.getByRole('list', { name: '作業記録一覧' })
      expect(within(list).getByText('1時間31分')).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('取得した一覧に記録中の作業記録があると終了操作を表示します', async () => {
    const timeRecords = createTimeRecordApiMock({
      getTimeRecords: async () => [{
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: null,
      }],
    })

    render(<TimeRecorder timeRecords={timeRecords} />)

    expect(await screen.findByRole('button', { name: '記録を終了' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '記録を開始' })).not.toBeInTheDocument()
  })

  it('開始ボタンを押すと記録開始を要求します', () => {
    const startTimeRecord = vi.fn(async () => ({
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
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
        endedAt: null,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const startedAtDisplay = await screen.findByLabelText('開始時刻')
    expect(startedAtDisplay).toHaveAttribute('datetime', 開始時刻)
    expect(startedAtDisplay).toHaveTextContent(表示用開始時刻)
  })

  it('記録を開始すると開始した記録が一覧に表示されます', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: null,
      }),
    })
    render(<TimeRecorder timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const list = screen.getByRole('list', { name: '作業記録一覧' })
    expect(await within(list).findByText(表示用開始時刻)).toBeInTheDocument()
  })

  it('記録を開始すると終了操作へ切り替わります', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: null,
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
        endedAt: null,
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
        endedAt: null,
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
    expect(endedAtDisplay).toHaveTextContent(表示用終了時刻)
  })

  it('記録を終了すると開始時刻から終了時刻までを作業時間として表示します', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: null,
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

    const recorder = screen.getByRole('heading', { name: 'プロジェクト未登録' }).closest('section')!
    expect(await within(recorder).findByText('作業時間')).toBeInTheDocument()
    expect(within(recorder).getByText('1時間30分')).toBeInTheDocument()
  })

  it('記録を終了すると開始操作へ切り替わります', async () => {
    const timeRecords = createTimeRecordApiMock({
      startTimeRecord: async () => ({
        id: 記録ID,
        startedAt: 開始時刻,
        endedAt: null,
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
        endedAt: null,
      })
      .mockResolvedValueOnce({
        id: 次の記録ID,
        startedAt: 次の開始時刻,
        endedAt: null,
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
