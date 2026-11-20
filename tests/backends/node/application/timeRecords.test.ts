import { beforeEach, describe, test } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { TimeRecords } from '../../../../src/backends/node/application/timeRecords.js'
import type { TimeRecordState } from '../../../../src/backends/node/application/timeRecords.js'

const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
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
    newId: () => String(nextId++), now: () => now,
  })
})

test('開始するたびに新しい識別子が発行され、現在時刻を開始時刻とする記録が返されます', () => {
  const first = tested.start()
  const second = tested.start()

  first.should.toStrictEqual(new TimeRecord('1', 開始時刻, null))
  second.should.toStrictEqual(new TimeRecord('2', 開始時刻, null))
})

describe('二件の記録を開始したとき', () => {
  let first: TimeRecord
  let second: TimeRecord

  beforeEach(() => {
    first = tested.start()
    second = tested.start()
  })

  test('一覧には開始した記録が保存順に返されます', () => {
    tested.list().should.toStrictEqual([first, second])
  })

  test('複数の記録のうち一件を指定して終了すると、現在時刻を終了時刻とするその記録が返されます', () => {
    now = 終了時刻

    const ended = tested.end(first.id)

    ;({ ended }).should.toStrictEqual({ ended: first.end(終了時刻) })
  })

  test('一件目を指定して終了すると一覧ではその記録だけが終了しています', () => {
    now = 終了時刻

    tested.end(first.id)

    tested.list().should.toStrictEqual([first.end(終了時刻), second])
  })
})

describe('一件目が終了済みで二件目は記録中のとき', () => {
  let 二件目: TimeRecord

  beforeEach(() => {
    二件目 = new TimeRecord('2', 開始時刻, null)
    state.add(new TimeRecord('1', 開始時刻, 終了時刻))
    state.add(二件目)
    now = 再終了時刻
  })

  test('終了済みの記録を再度指定すると終了時刻を更新した記録を返します', () => {
    const ended = tested.end('1')

    ;({ ended }).should.toStrictEqual({ ended: new TimeRecord('1', 開始時刻, 再終了時刻) })
  })

  test('終了済みの記録を再度指定すると一覧ではその記録だけの終了時刻が更新されます', () => {
    tested.end('1')

    tested.list().should.toStrictEqual([new TimeRecord('1', 開始時刻, 再終了時刻), 二件目])
  })
})
