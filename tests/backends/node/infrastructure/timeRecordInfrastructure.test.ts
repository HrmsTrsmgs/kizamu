import { beforeEach, describe, test, vi } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { TimeRecordNotFoundError } from '../../../../src/backends/node/application/timeRecords.js'
import { InMemoryTimeRecordState } from '../../../../src/backends/node/infrastructure/timeRecordState.js'
import { timeRecordSources } from '../../../../src/backends/node/infrastructure/timeRecordSources.js'

describe('メモリ上の記録', () => {
  const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
  const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')
  let tested: InMemoryTimeRecordState
  let first: TimeRecord
  let second: TimeRecord
  let ended: TimeRecord

  beforeEach(() => {
    tested = new InMemoryTimeRecordState()
    first = TimeRecord.start('first', 開始時刻)
    second = TimeRecord.start('second', 開始時刻)
    tested.add(first)
    tested.add(second)
    ended = first.end(終了時刻)
  })

  test('保存した記録を終了済みの記録に置き換えると、同じ識別子で終了済みの記録を取得できます', () => {
    tested.replace(ended)

    ;({ record: tested.find('first') }).should.toStrictEqual({ record: ended })
  })

  test('保存した記録を終了済みの記録に置き換えても、一覧には追加した順に並びます', () => {
    tested.replace(ended)

    tested.all().should.toStrictEqual([ended, second])
  })

  test('未保存の記録を更新するとTimeRecordNotFoundErrorが投げられ、保存済みの記録は変わりません', () => {
    const 未保存の記録 = TimeRecord.start('unsaved', 開始時刻)

    const 更新する = () => tested.replace(未保存の記録)
    更新する.should.throw(TimeRecordNotFoundError)

    tested.all().should.toStrictEqual([first, second])
  })
})

test('記録用の時計はシステムの現在時刻を返します', () => {
  vi.useFakeTimers({ toFake: ['Date'] })
  try {
    vi.setSystemTime(new Date('2026-08-01T09:00:00Z'))

    timeRecordSources.now().should.deep.equal(Temporal.Instant.from('2026-08-01T09:00:00Z'))
  } finally {
    vi.useRealTimers()
  }
})

test('新しい記録の識別子にはバージョン4のUUIDを生成します', () => {
  timeRecordSources.newId().should.match(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i)
})
