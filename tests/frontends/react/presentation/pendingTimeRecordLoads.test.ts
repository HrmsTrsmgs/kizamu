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

  it('複数の記録一覧を並行して取得すると、各要求の開始後に届いた記録の更新だけが到着順に残ります', () => {
    const 先の取得 = tested.start()
    tested.recordUpdate(記録)
    const 後の取得 = tested.start()
    tested.recordUpdate(終了済み記録)
    tested.recordUpdate(次の記録)

    先の取得.updatedRecords.should.toStrictEqual([記録, 終了済み記録, 次の記録])
    後の取得.updatedRecords.should.toStrictEqual([終了済み記録, 次の記録])
  })

  it('記録一覧の取得が一件完了すると、それ以降の記録の更新は取得中の要求だけに加わります', () => {
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
