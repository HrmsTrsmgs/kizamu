import { beforeEach, describe, test, vi } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { InMemoryTimeRecordState } from '../../../src/backends/node/infrastructure/timeRecordState.js'
import { timeRecordSources } from '../../../src/backends/node/infrastructure/timeRecordSources.js'

describe('メモリ上の記録', () => {
  const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
  const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')
  let tested: InMemoryTimeRecordState
  let first: TimeRecord
  let second: TimeRecord

  beforeEach(() => {
    tested = new InMemoryTimeRecordState()
    first = TimeRecord.start('first', 開始時刻)
    second = TimeRecord.start('second', 開始時刻)
    tested.add(first)
    tested.add(second)
  })

  test('置換した記録を識別子で取得できます', () => {
    const ended = first.end(終了時刻)

    tested.replace(ended)

    ;({ record: tested.find('first') }).should.toStrictEqual({ record: ended })
  })

  test('置換後も一覧では記録の追加順を保ちます', () => {
    const ended = first.end(終了時刻)

    tested.replace(ended)

    tested.all().should.toStrictEqual([ended, second])
  })
})

test('時刻の取得元は現在の日時点を返します', () => {
  vi.useFakeTimers({ toFake: ['Date'] })
  try {
    vi.setSystemTime(new Date('2026-08-01T09:00:00Z'))

    timeRecordSources.now().should.deep.equal(Temporal.Instant.from('2026-08-01T09:00:00Z'))
  } finally {
    vi.useRealTimers()
  }
})

test('識別子の生成元はバージョン4のUUIDを返します', () => {
  timeRecordSources.newId().should.match(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i)
})
