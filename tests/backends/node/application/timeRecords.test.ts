import { beforeEach, describe, test } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { TimeRecords } from '../../../../src/backends/node/application/timeRecords.js'
import type { TimeRecordState } from '../../../../src/backends/node/application/timeRecords.js'

const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
const 次の開始時刻 = Temporal.Instant.from('2026-08-01T09:30:00Z')
const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')
const 再終了時刻 = Temporal.Instant.from('2026-08-01T11:00:00Z')

function createState(): TimeRecordState {
  const records: TimeRecord[] = []
  return {
    add: (record) => { records.push(record) },
    find: (id) => records.find((record) => record.id === id),
    replace: (record) => {
      const index = records.findIndex((existing) => existing.id === record.id)
      if (index !== -1) records[index] = record
    },
    all: () => records,
  }
}

let state: TimeRecordState
let now: Temporal.Instant
let tested: TimeRecords

beforeEach(() => {
  state = createState()
  let nextId = 1
  now = 開始時刻
  tested = new TimeRecords(state, {
    newId: () => `issued-${nextId++}`, now: () => now,
  })
})

test('開始を要求するたびに発行元の識別子とその時点の現在時刻が返されます', () => {
  const first = tested.start()
  now = 次の開始時刻
  const second = tested.start()

  first.id.should.equal('issued-1')
  first.startedAt.should.deep.equal(開始時刻)
  second.id.should.equal('issued-2')
  second.startedAt.should.deep.equal(次の開始時刻)
})

test('二件の記録を開始すると、一覧には開始した記録が保存順に返されます', () => {
  const first = tested.start()
  const second = tested.start()

  tested.list().should.toStrictEqual([first, second])
})

describe.each([
  { status: '記録中', previousEndedAt: null, requestedAt: 終了時刻 },
  { status: '終了済み', previousEndedAt: 終了時刻, requestedAt: 再終了時刻 },
])('$statusの記録が先に保存されているとき', ({ previousEndedAt, requestedAt }) => {
  let 最初の記録: TimeRecord
  let 次の記録: TimeRecord

  beforeEach(() => {
    最初の記録 = new TimeRecord('1', 開始時刻, previousEndedAt)
    次の記録 = new TimeRecord('2', 次の開始時刻, null)
    state.add(最初の記録)
    state.add(次の記録)
    now = requestedAt
  })

  test('先に保存した記録を終了すると、その識別子と開始時刻と今回の終了時刻が返されます', () => {
    const 終了結果 = { ended: tested.end(最初の記録.id) }

    終了結果.should.toStrictEqual({ ended: new TimeRecord(最初の記録.id, 開始時刻, requestedAt) })
  })

  test('先に保存した記録を終了すると、一覧ではその記録だけに今回の終了時刻が反映されます', () => {
    tested.end(最初の記録.id)

    tested.list().should.toStrictEqual([new TimeRecord(最初の記録.id, 開始時刻, requestedAt), 次の記録])
  })
})
