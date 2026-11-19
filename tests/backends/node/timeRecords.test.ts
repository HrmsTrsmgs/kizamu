import { beforeEach, test } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { TimeRecords } from '../../../src/backends/node/application/timeRecords.js'
import type { TimeRecordState } from '../../../src/backends/node/application/timeRecords.js'

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

test('開始するたびに生成された識別子と現在時刻で開始した記録を返します', () => {
  const first = tested.start()
  const second = tested.start()

  first.should.toStrictEqual(new TimeRecord('1', 開始時刻, null))
  second.should.toStrictEqual(new TimeRecord('2', 開始時刻, null))
})

test('一覧には開始した記録が保存順に返されます', () => {
  const first = tested.start()
  const second = tested.start()

  tested.list().should.toStrictEqual([first, second])
})

test('複数の記録があるときは指定した記録の終了結果を返します', () => {
  const first = tested.start()
  tested.start()
  now = 終了時刻

  const ended = tested.end(first.id)

  ;({ ended }).should.toStrictEqual({ ended: first.end(終了時刻) })
})

test('一件目を指定して終了すると一覧ではその記録だけが終了しています', () => {
  const first = tested.start()
  const second = tested.start()
  now = 終了時刻

  tested.end(first.id)

  tested.list().should.toStrictEqual([first.end(終了時刻), second])
})

test('終了済みの記録を再度指定すると終了時刻を更新した記録を返します', () => {
  終了済みと記録中の二件を保存する()
  now = 再終了時刻

  const ended = tested.end('1')

  ;({ ended }).should.toStrictEqual({ ended: new TimeRecord('1', 開始時刻, 再終了時刻) })
})

test('終了済みの記録を再度指定すると一覧ではその記録だけの終了時刻が更新されます', () => {
  const 二件目 = 終了済みと記録中の二件を保存する()
  now = 再終了時刻

  tested.end('1')

  tested.list().should.toStrictEqual([new TimeRecord('1', 開始時刻, 再終了時刻), 二件目])
})

function 終了済みと記録中の二件を保存する() {
  const 二件目 = new TimeRecord('2', 開始時刻, null)
  state.add(new TimeRecord('1', 開始時刻, 終了時刻))
  state.add(二件目)
  return 二件目
}
