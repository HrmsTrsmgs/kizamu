import { test } from 'vitest'
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

test('開始と一覧は生成された識別子と時刻を保存順に返します', () => {
  let id = 0
  const tested = new TimeRecords(createState(), {
    newId: () => String(++id), now: () => 開始時刻,
  })
  const first = tested.start()
  const second = tested.start()

  first.should.include({ id: '1', startedAt: 開始時刻, endedAt: null })
  second.should.include({ id: '2', startedAt: 開始時刻, endedAt: null })
  tested.list().should.deep.equal([first, second])
})

test('一件目を指定して終了すると二件目は変化しません', () => {
  let id = 0
  let now = 開始時刻
  const tested = new TimeRecords(createState(), {
    newId: () => String(++id), now: () => now,
  })
  const first = tested.start()
  const second = tested.start()
  now = 終了時刻

  const ended = tested.end(first.id)

  const expectedFirst = first.end(終了時刻)
  ;({ ended }).should.have.property('ended').that.deep.equal(expectedFirst)
  tested.list().should.deep.equal([expectedFirst, second])
})

test('終了済みの一件目を再度終了すると終了時刻が更新され二件目は変化しません', () => {
  const 再終了時刻 = Temporal.Instant.from('2026-08-01T11:00:00Z')
  const 二件目 = new TimeRecord('2', 開始時刻, null)
  const tested = new TimeRecords(
    createState([new TimeRecord('1', 開始時刻, 終了時刻), 二件目]),
    { newId: () => '3', now: () => 再終了時刻 },
  )
  const 再終了後の一件目 = new TimeRecord('1', 開始時刻, 再終了時刻)

  ;({ ended: tested.end('1') }).should.have.property('ended').that.deep.equal(再終了後の一件目)
  tested.list().should.deep.equal([再終了後の一件目, 二件目])
})
