import { Temporal } from 'temporal-polyfill'
import { beforeEach, describe, it } from 'vitest'
import { TimeRecord } from 'kizamu-domain-typescript'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')
const 次の終了時刻 = Temporal.Instant.from('2026-08-01T11:00:00Z')

describe('TimeRecord', () => {
  it('記録を開始すると、指定した識別子と開始時刻を持つ、終了時刻が未設定の記録が得られます', () => {
    TimeRecord.start(記録ID, 開始時刻).should.toStrictEqual(new TimeRecord(記録ID, 開始時刻, null))
  })

  describe('開始した記録', () => {
    let record: TimeRecord

    beforeEach(() => {
      record = TimeRecord.start(記録ID, 開始時刻)
    })

    it('記録中の経過時間は、日付をまたいでも開始から指定時刻までの通算時間です', () => {
      const 翌日の基準時刻 = 開始時刻.add({ hours: 26, minutes: 15 })

      record.durationAt(翌日の基準時刻).should.deep.equal(Temporal.Duration.from({ hours: 26, minutes: 15 }))
    })

    it('記録を終了すると、元の識別子と開始時刻を保ち、指定時刻を終了時刻とする新しい記録が返されます', () => {
      const endedRecord = record.end(終了時刻)

      endedRecord.should.not.equal(record)
      endedRecord.should.toStrictEqual(new TimeRecord(記録ID, 開始時刻, 終了時刻))
    })

    it('記録を終了しても元の記録の終了時刻は空のままです', () => {
      record.end(終了時刻)

      record.should.have.property('endedAt', null)
    })
  })

  it('既存の識別子と開始時刻と終了時刻から記録を復元できます', () => {
    const record = new TimeRecord(記録ID, 開始時刻, 終了時刻)

    record.id.should.equal(記録ID)
    record.startedAt.should.deep.equal(開始時刻)
    record.should.have.property('endedAt').that.deep.equal(終了時刻)
  })

  describe('復元した終了済みの記録', () => {
    let record: TimeRecord

    beforeEach(() => {
      record = new TimeRecord(記録ID, 開始時刻, 終了時刻)
    })

    it('終了済みの記録の経過時間は、指定時刻が終了後でも開始から終了までの長さです', () => {
      record.durationAt(次の終了時刻).should.deep.equal(Temporal.Duration.from({ hours: 1, minutes: 30 }))
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
