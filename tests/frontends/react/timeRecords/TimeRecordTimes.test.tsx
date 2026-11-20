import { render, screen } from '@testing-library/react'
import { beforeEach, describe, it } from 'vitest'
import TimeRecordTimes from '../../../../src/frontends/react/src/timeRecords/TimeRecordTimes'
import { formatHtmlDateTime } from '../../../../src/frontends/react/src/timeRecords/htmlDateTime'
import { presentedRecord, timestamp } from '../test/timeRecords'

describe('TimeRecordTimes', () => {
  describe('時差付きの開始・終了時刻がある記録', () => {
    const 開始時刻 = '2026-08-01T18:00:00.0000000+09:00'
    const 終了時刻 = '2026-08-01T19:30:00.0000000+09:00'

    beforeEach(() => {
      render(<TimeRecordTimes record={presentedRecord('記録', timestamp(開始時刻), timestamp(終了時刻)).record} labelled />)
    })

    it('開始と終了のdatetime属性に元の日時点をHTMLの日時形式で設定します', () => {
      screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', '2026-08-01T09:00:00.000Z')
      screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', '2026-08-01T10:30:00.000Z')
    })

    it('開始と終了を日本時間の分単位で表示します', () => {
      screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
      screen.getByLabelText('終了時刻').should.toHaveTextContent('2026/08/01 19:30')
    })

    it('開始から終了までの作業時間を表示します', () => {
      screen.getByText('1時間30分').should.toBeInTheDocument()
    })
  })
})

describe('formatHtmlDateTime', () => {
  it.each([
    { isoText: '2026-08-01T23:59:59.9999999Z', expected: '2026-08-01T23:59:59.999Z' },
    { isoText: '2026-08-02T00:00:59.9999999Z', expected: '2026-08-02T00:00:59.999Z' },
  ])('$isoText をHTML形式にすると、秒や日付を繰り上げずに小数秒を3桁で切り捨てます', ({ isoText, expected }) => {
    formatHtmlDateTime(timestamp(isoText).instant).should.equal(expected)
  })
})
