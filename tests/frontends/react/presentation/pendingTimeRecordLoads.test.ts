// @vitest-environment node
import { beforeEach, describe, it } from 'vitest'
import { PendingTimeRecordLoads } from '../../../../src/frontends/react/src/presentation/pendingTimeRecordLoads'
import { presentedRecord, timestamp } from '../test/timeRecords'

const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 記録 = presentedRecord('記録', 開始時刻)
const 終了済み記録 = presentedRecord('記録', 開始時刻, timestamp('2026-08-01T10:30:00Z'))
const 次の記録 = presentedRecord('次の記録', timestamp('2026-08-01T11:00:00Z'))

describe('PendingTimeRecordLoads', () => {
  let tested: PendingTimeRecordLoads

  beforeEach(() => {
    tested = new PendingTimeRecordLoads()
  })

  it('各取得に対して開始後に届いた更新だけを応答順に保持します', () => {
    const 先の取得 = tested.start()
    tested.recordUpdate(記録)
    const 後の取得 = tested.start()
    tested.recordUpdate(終了済み記録)
    tested.recordUpdate(次の記録)

    先の取得.updatedRecords.should.toStrictEqual([記録, 終了済み記録, 次の記録])
    後の取得.updatedRecords.should.toStrictEqual([終了済み記録, 次の記録])
  })

  it('取得が終わると残りの取得だけが後続の更新を受け取ります', () => {
    const 先の取得 = tested.start()
    const 後の取得 = tested.start()
    tested.recordUpdate(記録)

    後の取得.finish()
    tested.recordUpdate(終了済み記録)
    先の取得.finish()
    tested.recordUpdate(次の記録)

    後の取得.updatedRecords.should.toStrictEqual([記録])
    先の取得.updatedRecords.should.toStrictEqual([記録, 終了済み記録])
  })
})
