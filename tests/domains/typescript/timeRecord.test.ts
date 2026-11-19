import { Temporal } from 'temporal-polyfill'
import { beforeEach, describe, it } from 'vitest'
import { TimeRecord } from 'kizamu-domain-typescript'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')
const 次の終了時刻 = Temporal.Instant.from('2026-08-01T11:00:00Z')

describe('TimeRecord', () => {
  describe('開始した記録', () => {
    let record: TimeRecord

    beforeEach(() => {
      record = TimeRecord.start(記録ID, 開始時刻)
    })

    it('指定した識別子と日時点で終了していない記録を開始します', () => {
      record.should.toStrictEqual(new TimeRecord(記録ID, 開始時刻, null))
    })

    it('識別子と開始時刻を保ち指定した日時点で終了した新しい記録を返します', () => {
      const endedRecord = record.end(終了時刻)

      endedRecord.should.not.equal(record)
      endedRecord.should.toStrictEqual(new TimeRecord(記録ID, 開始時刻, 終了時刻))
    })

    it('記録を終了しても元の記録の終了時刻は空のままです', () => {
      record.end(終了時刻)

      record.should.have.property('endedAt', null)
    })
  })

  describe('復元した終了済みの記録', () => {
    let record: TimeRecord

    beforeEach(() => {
      record = new TimeRecord(記録ID, 開始時刻, 終了時刻)
    })

    it('既存の識別子と開始時刻と終了時刻から記録を復元できます', () => {
      record.id.should.equal(記録ID)
      record.startedAt.should.deep.equal(開始時刻)
      record.should.have.property('endedAt').that.deep.equal(終了時刻)
    })

    it('終了済みの記録を再び終了すると終了時刻を更新した新しい記録が返されます', () => {
      const endedRecord = record.end(次の終了時刻)

      endedRecord.should.not.equal(record)
      endedRecord.should.toStrictEqual(new TimeRecord(記録ID, 開始時刻, 次の終了時刻))
    })

    it('終了済みの記録を再び終了しても元の記録には以前の終了時刻が残ります', () => {
      record.end(次の終了時刻)

      record.should.have.property('endedAt').that.deep.equal(終了時刻)
    })
  })
})
