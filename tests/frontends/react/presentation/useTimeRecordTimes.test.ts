import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, it, vi } from 'vitest'
import { useTimeRecordTimes } from '../../../../src/frontends/react/src/presentation/useTimeRecordTimes'
import { TimeRecord } from 'kizamu-domain-typescript'
import { Temporal } from 'temporal-polyfill'
import { timestamp } from '../test/timeRecords'

const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 記録 = TimeRecord.start('記録', 開始時刻.instant)
const 開始から1時間31分後の1秒前 = '2026-08-01T10:30:59Z'
const 開始から2時間15分後の終了時刻 = '2026-08-01T11:15:00Z'

describe('useTimeRecordTimes', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(終了時刻.isoText)
  })
  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('開始と終了の日時点を日本時間の分単位に整形します', () => {
    const { result } = renderHook(() => useTimeRecordTimes(記録.end(終了時刻.instant)))

    result.current.startedAtText.should.equal('2026/08/01 18:00')
    result.current.should.have.property('endedAtText', '2026/08/01 19:30')
  })

  it.each([
    { endedAt: 終了時刻, expectedDuration: '1時間30分' },
    { endedAt: timestamp(開始から2時間15分後の終了時刻), expectedDuration: '2時間15分' },
  ])('終了済みの記録の作業時間は現在時刻を参照せず、開始から終了までの $expectedDuration です', ({ endedAt, expectedDuration }) => {
    const clock = vi.spyOn(Temporal.Now, 'instant')
    const { result } = renderHook(() => useTimeRecordTimes(記録.end(endedAt.instant)))

    clock.should.have.callCount(0)

    result.current.durationText.should.equal(expectedDuration)
  })

  describe.each([
    { currentTime: 終了時刻.isoText, expectedDuration: '1時間30分' },
    { currentTime: 開始から2時間15分後の終了時刻, expectedDuration: '2時間15分' },
  ])('現在時刻が $currentTime の記録中の表示', ({ currentTime, expectedDuration }) => {
    let view: ReturnType<typeof 記録中の時刻を表示する>

    beforeEach(() => {
      vi.setSystemTime(currentTime)
      view = 記録中の時刻を表示する()
    })

    it('記録中の作業時間は開始から現在時刻までの長さです', () => {
      view.result.current.durationText.should.equal(expectedDuration)
    })

    it('記録中は表示用の終了時刻が未設定です', () => {
      view.result.current.should.have.property('endedAtText', null)
    })
  })

  describe('記録中の時刻を表示したとき', () => {
    let view: ReturnType<typeof 記録中の時刻を表示する>

    beforeEach(() => {
      view = 記録中の時刻を表示する()
    })

    it('記録中に時間が経過すると作業時間を更新します', async () => {
      view.result.current.durationText.should.equal('1時間30分')

      await act(() => vi.advanceTimersByTimeAsync(60_000))

      view.result.current.durationText.should.equal('1時間31分')
    })

    it('終了済みの記録の作業時間は時計が進んでも変わりません', async () => {
      view.rerender(記録.end(終了時刻.instant))

      await act(() => vi.advanceTimersByTimeAsync(60_000))

      view.result.current.durationText.should.equal('1時間30分')
    })

    it('記録が終了したら時刻の更新タイマーを解除します', async () => {
      view.rerender(記録.end(終了時刻.instant))
      await act(() => vi.advanceTimersByTimeAsync(60_000))

      vi.getTimerCount().should.equal(0)
    })

    it('記録画面から離れると、作業時間の更新タイマーを解除します', () => {
      vi.getTimerCount().should.equal(1)

      view.unmount()

      vi.getTimerCount().should.equal(0)
    })
  })

  it('開始からの経過時間が次の分に達した時点で、作業時間の表示が1分増えます', async () => {
    vi.setSystemTime(開始から1時間31分後の1秒前)
    const { result } = renderHook(() => useTimeRecordTimes(記録))
    result.current.durationText.should.equal('1時間30分')

    await act(() => vi.advanceTimersByTimeAsync(1_000))

    result.current.durationText.should.equal('1時間31分')
  })
})

function 記録中の時刻を表示する() {
  return renderHook(useTimeRecordTimes, { initialProps: 記録 })
}
