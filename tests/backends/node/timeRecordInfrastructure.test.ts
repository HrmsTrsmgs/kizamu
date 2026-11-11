import { test, vi } from 'vitest'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'
import { InMemoryTimeRecordState } from '../../../src/backends/node/infrastructure/timeRecordState.js'
import { timeRecordSources } from '../../../src/backends/node/infrastructure/timeRecordSources.js'

test('メモリ上の記録を識別子で取得し、置換しても順序を保ちます', () => {
  const tested = new InMemoryTimeRecordState()
  const start = Temporal.Instant.from('2026-08-01T09:00:00Z')
  const first = TimeRecord.start('first', start)
  const second = TimeRecord.start('second', start)
  tested.add(first)
  tested.add(second)
  const ended = first.end(Temporal.Instant.from('2026-08-01T10:30:00Z'))

  tested.replace(ended)

  ;({ record: tested.find('first') }).should.toStrictEqual({ record: ended })
  tested.all().should.toStrictEqual([ended, second])
})

test('時刻と識別子を生成します', () => {
  vi.useFakeTimers({ toFake: ['Date'] })
  try {
    vi.setSystemTime(new Date('2026-08-01T09:00:00Z'))
    timeRecordSources.now().should.deep.equal(Temporal.Instant.from('2026-08-01T09:00:00Z'))
    timeRecordSources.newId().should.match(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i)
  } finally {
    vi.useRealTimers()
  }
})
