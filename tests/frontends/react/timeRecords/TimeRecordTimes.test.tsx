import { render, screen } from '@testing-library/react'
import { describe, it } from 'vitest'
import TimeRecordTimes from '../../../../src/frontends/react/src/timeRecords/TimeRecordTimes'
import { timestamp } from '../test/timeRecords'

describe('TimeRecordTimes', () => {
  it('元のISO表記をtime要素に保持し日本時間を表示します', () => {
    const 開始時刻 = '2026-08-01T18:00:00.0000000+09:00'
    const 終了時刻 = '2026-08-01T19:30:00.0000000+09:00'

    render(<TimeRecordTimes startedAt={timestamp(開始時刻)} endedAt={timestamp(終了時刻)} labelled />)

    screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', 開始時刻)
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
    screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', 終了時刻)
    screen.getByLabelText('終了時刻').should.toHaveTextContent('2026/08/01 19:30')
    screen.getByText('1時間30分').should.toBeInTheDocument()
  })
})
