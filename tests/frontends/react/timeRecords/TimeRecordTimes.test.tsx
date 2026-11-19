import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
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
      screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', expect.stringMatching(/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:\d{2})$/))
      Date.parse(screen.getByLabelText('開始時刻').getAttribute('datetime') ?? '').should.equal(Date.parse(開始時刻))
      screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', expect.stringMatching(/^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:\d{2})$/))
      Date.parse(screen.getByLabelText('終了時刻').getAttribute('datetime') ?? '').should.equal(Date.parse(終了時刻))
    })

    it('開始と終了を日本時間の分単位で表示します', () => {
      screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 18:00')
      screen.getByLabelText('終了時刻').should.toHaveTextContent('2026/08/01 19:30')
    })

    it('開始から終了までの作業時間を表示します', () => {
      screen.getByText('1時間30分').should.toBeInTheDocument()
    })
  })

  it('開始・終了時刻のdatetime属性では、小数秒を3桁で切り捨てるため、秒や日付は繰り上がりません', () => {
    const 開始時刻 = '2026-08-01T23:59:59.9999999Z'
    const 終了時刻 = '2026-08-02T00:00:59.9999999Z'

    render(<TimeRecordTimes record={presentedRecord('記録', timestamp(開始時刻), timestamp(終了時刻)).record} labelled />)

    screen.getByLabelText('開始時刻').should.toHaveAttribute('datetime', '2026-08-01T23:59:59.999Z')
    screen.getByLabelText('終了時刻').should.toHaveAttribute('datetime', '2026-08-02T00:00:59.999Z')
  })
})

describe('formatHtmlDateTime', () => {
  it('日付が変わる直前の時刻をHTML形式にすると、小数秒を3桁で切り捨てて同じ日付のまま返します', () => {
    formatHtmlDateTime(timestamp('2026-08-01T23:59:59.9999999Z').instant)
      .should.equal('2026-08-01T23:59:59.999Z')
  })
})
