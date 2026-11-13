import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import TimeRecordTimes from '../../../../src/frontends/react/src/timeRecords/TimeRecordTimes'
import { timestamp } from '../test/timeRecords'

describe('TimeRecordTimes', () => {
  it('時刻要素に元の日時を設定し日本時間を表示します', () => {
    const 開始時刻 = '2026-08-01T18:00:00.0000000+09:00'
    const 終了時刻 = '2026-08-01T19:30:00.0000000+09:00'

    render(<TimeRecordTimes startedAt={timestamp(開始時刻)} endedAt={timestamp(終了時刻)} labelled />)

    screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', expect.stringMatching(/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:\d{2})$/))
    Date.parse(screen.getByLabelText('開始時刻').getAttribute('datetime') ?? '').should.equal(Date.parse(開始時刻))
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
    screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', expect.stringMatching(/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:\d{2})$/))
    Date.parse(screen.getByLabelText('終了時刻').getAttribute('datetime') ?? '').should.equal(Date.parse(終了時刻))
    screen.getByLabelText('終了時刻').should.toHaveTextContent('2026/08/01 19:30')
    screen.getByText('1時間30分').should.toBeInTheDocument()
  })

  it('小数秒が3桁を超える開始時刻も時刻要素に有効な日時として設定します', () => {
    const 開始時刻 = '2026-08-01T18:00:00.1230000+09:00'

    render(<TimeRecordTimes startedAt={timestamp(開始時刻)} endedAt={null} labelled />)

    screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', expect.stringMatching(/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:\d{2})$/))
    Date.parse(screen.getByLabelText('開始時刻').getAttribute('datetime') ?? '').should.equal(Date.parse(開始時刻))
  })
})
