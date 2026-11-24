import { render, screen, within } from '@testing-library/react'
import { TimeRecord } from 'kizamu-domain-typescript'
import { describe, it } from 'vitest'
import TimeRecordList from '../../../../src/frontends/react/src/timeRecords/TimeRecordList'
import { timestamp } from '../test/timeRecords'

const 記録 = TimeRecord.start('record', timestamp('2026-08-01T09:00:00Z').instant)
const 終了済み記録 = 記録.end(timestamp('2026-08-01T10:30:00Z').instant)

describe('TimeRecordList', () => {
  it('二つの記録一覧はそれぞれの見出しを参照します', () => {
    render(
      <>
        <TimeRecordList records={[]} />
        <TimeRecordList records={[]} />
      </>,
    )

    const lists = screen.getAllByRole('region', { name: '作業記録' })
    const headingIds = lists.map(list => within(list).getByRole('heading', { name: '作業記録' }).id)

    lists.should.have.length(2)
    new Set(headingIds).size.should.equal(2)
    lists.map(list => list.getAttribute('aria-labelledby')).should.deep.equal(headingIds)
  })

  it('終了済みの記録が一件あると、画面に「1件」と表示されます', () => {
    render(<TimeRecordList records={[終了済み記録]} />)

    screen.getByText('1件').should.toBeInTheDocument()
  })

  it('作業記録一覧には、進行中の記録を「記録中」として表示します', () => {
    render(<TimeRecordList records={[記録]} />)

    within(screen.getByRole('list', { name: '作業記録一覧' })).getByText('記録中').should.toBeInTheDocument()
  })
})
