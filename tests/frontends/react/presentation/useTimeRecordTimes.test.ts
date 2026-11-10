import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, it, vi } from 'vitest'
import { useTimeRecordTimes } from '../../../../src/frontends/react/src/presentation/useTimeRecordTimes'
import type { RecordTimestamp } from '../../../../src/frontends/react/src/presentation/timeRecordPresentation'
import { timestamp } from '../test/timeRecords'

const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 開始から1時間31分後の1秒前 = '2026-08-01T10:30:59Z'
const 開始から2時間15分後の終了時刻 = '2026-08-01T11:15:00Z'

describe('useTimeRecordTimes', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(終了時刻.isoText)
  })
  afterEach(() => vi.useRealTimers())

  it('開始と終了の日時点を日本時間の分単位に整形します', () => {
    const { result } = renderHook(() => useTimeRecordTimes(開始時刻, 終了時刻))

    result.current.startedAtText.should.equal('2026/08/01 18:00')
    result.current.should.have.property('endedAtText', '2026/08/01 19:30')
  })

  it.each([
    { endedAt: 終了時刻, expectedDuration: '1時間30分' },
    { endedAt: timestamp(開始から2時間15分後の終了時刻), expectedDuration: '2時間15分' },
  ])('終了済み記録では終了時刻までを作業時間とします: $expectedDuration', ({ endedAt, expectedDuration }) => {
    const { result } = renderHook(() => useTimeRecordTimes(開始時刻, endedAt))

    result.current.durationText.should.equal(expectedDuration)
  })

  it.each([
    { currentTime: 終了時刻.isoText, expectedDuration: '1時間30分' },
    { currentTime: 開始から2時間15分後の終了時刻, expectedDuration: '2時間15分' },
  ])('記録中は現在時刻までを作業時間とします: $expectedDuration', ({ currentTime, expectedDuration }) => {
    vi.setSystemTime(currentTime)

    const { result } = renderHook(() => useTimeRecordTimes(開始時刻, null))

    result.current.durationText.should.equal(expectedDuration)
    result.current.should.have.property('endedAtText', null)
  })

  it('記録中に時間が経過すると作業時間を更新します', async () => {
    const { result } = renderHook(() => useTimeRecordTimes(開始時刻, null))
    result.current.durationText.should.equal('1時間30分')

    await act(() => vi.advanceTimersByTimeAsync(60_000))

    result.current.durationText.should.equal('1時間31分')
  })

  it('作業時間が次の整数分になると表示用の時間を更新します', async () => {
    vi.setSystemTime(開始から1時間31分後の1秒前)
    const { result } = renderHook(() => useTimeRecordTimes(開始時刻, null))
    result.current.durationText.should.equal('1時間30分')

    await act(() => vi.advanceTimersByTimeAsync(1_000))

    result.current.durationText.should.equal('1時間31分')
  })

  it('終了した記録の時間は時刻が進んでも変わりません', async () => {
    const initialProps: { endedAt: RecordTimestamp | null } = { endedAt: null }
    const { result, rerender } = renderHook(({ endedAt }) => useTimeRecordTimes(開始時刻, endedAt), {
      initialProps,
    })
    rerender({ endedAt: 終了時刻 })

    await act(() => vi.advanceTimersByTimeAsync(60_000))

    result.current.durationText.should.equal('1時間30分')
    vi.getTimerCount().should.equal(0)
  })

  it('画面から離れると時刻の更新を解除します', () => {
    const { unmount } = renderHook(() => useTimeRecordTimes(開始時刻, null))
    vi.getTimerCount().should.equal(1)

    unmount()

    vi.getTimerCount().should.equal(0)
  })
})
