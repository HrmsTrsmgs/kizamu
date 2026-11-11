import { Temporal } from 'temporal-polyfill'
import { describe, it } from 'vitest'
import { TimeRecord } from 'kizamu-domain-typescript'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = Temporal.Instant.from('2026-08-01T09:00:00Z')
const 終了時刻 = Temporal.Instant.from('2026-08-01T10:30:00Z')

describe('TimeRecord', () => {
  it('指定した識別子と日時点で終了していない記録を開始します', () => {
    const record = TimeRecord.start(記録ID, 開始時刻)

    record.id.should.equal(記録ID)
    record.startedAt.should.deep.equal(開始時刻)
    record.should.have.property('endedAt', null)
  })

  it('識別子と開始時刻を保ったまま指定した日時点で終了します', () => {
    const record = TimeRecord.start(記録ID, 開始時刻)

    const endedRecord = record.end(終了時刻)

    endedRecord.should.not.equal(record)
    endedRecord.id.should.equal(記録ID)
    endedRecord.startedAt.should.deep.equal(開始時刻)
    endedRecord.should.have.property('endedAt').that.deep.equal(終了時刻)
    record.should.have.property('endedAt', null)
  })

  it('既存の識別子と開始時刻と終了時刻から記録を復元できます', () => {
    const record = new TimeRecord(記録ID, 開始時刻, 終了時刻)

    record.id.should.equal(記録ID)
    record.startedAt.should.deep.equal(開始時刻)
    record.should.have.property('endedAt').that.deep.equal(終了時刻)
  })

  it('終了済みの記録を再び終了すると元の記録を変えずに終了時刻を更新した記録が返されます', () => {
    const record = new TimeRecord(記録ID, 開始時刻, 終了時刻)
    const 次の終了時刻 = Temporal.Instant.from('2026-08-01T11:00:00Z')

    const endedRecord = record.end(次の終了時刻)

    endedRecord.should.not.equal(record)
    endedRecord.id.should.equal(記録ID)
    endedRecord.startedAt.should.deep.equal(開始時刻)
    endedRecord.should.have.property('endedAt').that.deep.equal(次の終了時刻)
    record.should.have.property('endedAt').that.deep.equal(終了時刻)
  })
})
