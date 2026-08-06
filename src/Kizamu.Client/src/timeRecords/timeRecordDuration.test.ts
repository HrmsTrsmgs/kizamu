import { Temporal } from 'temporal-polyfill'
import { describe, expect, it } from 'vitest'
import { calculateTimeRecordDuration } from './timeRecordDuration'

const 開始日時点 = Temporal.Instant.from('2026-08-01T09:00:00Z')
const 開始から1時間30分後の終了日時点 = Temporal.Instant.from('2026-08-01T10:30:00Z')

describe('calculateTimeRecordDuration', () => {
  it('開始日時点から終了日時点までを期間として返します', () => {
    const duration = calculateTimeRecordDuration(
      開始日時点,
      開始から1時間30分後の終了日時点,
    )

    expect(duration).toEqual(Temporal.Duration.from({ hours: 1, minutes: 30 }))
  })
})
