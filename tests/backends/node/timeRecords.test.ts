import { beforeEach, test } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { TimeRecords } from '../../../src/backends/node/application/timeRecords.js'
import type { TimeRecordState } from '../../../src/backends/node/application/timeRecords.js'

const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')

function createState(records: TimeRecord[] = []): TimeRecordState {
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

let records: TimeRecord[]
let id: number
let now: Temporal.Instant
let tested: TimeRecords

beforeEach(() => {
  records = []
  id = 0
  now = 開始時刻
  tested = new TimeRecords(createState(records), {
    newId: () => String(++id), now: () => now,
  })
})

test('開始と一覧は生成された識別子と時刻を保存順に返します', () => {
  const first = tested.start()
  const second = tested.start()

  first.should.toStrictEqual(new TimeRecord('1', 開始時刻, null))
  second.should.toStrictEqual(new TimeRecord('2', 開始時刻, null))
  tested.list().should.toStrictEqual([first, second])
})

test('一件目を指定して終了すると二件目は変化しません', () => {
  const first = tested.start()
  const second = tested.start()
  now = 終了時刻

  const ended = tested.end(first.id)

  const expectedFirst = first.end(終了時刻)
  ;({ ended }).should.toStrictEqual({ ended: expectedFirst })
  tested.list().should.toStrictEqual([expectedFirst, second])
})

test('終了済みの一件目を再度終了すると終了時刻が更新され二件目は変化しません', () => {
  const 再終了時刻 = Temporal.Instant.from('2026-08-01T11:00:00Z')
  const 二件目 = new TimeRecord('2', 開始時刻, null)
  records.push(new TimeRecord('1', 開始時刻, 終了時刻), 二件目)
  id = 2
  now = 再終了時刻
  const 再終了後の一件目 = new TimeRecord('1', 開始時刻, 再終了時刻)

  ;({ ended: tested.end('1') }).should.toStrictEqual({ ended: 再終了後の一件目 })
  tested.list().should.toStrictEqual([再終了後の一件目, 二件目])
})
