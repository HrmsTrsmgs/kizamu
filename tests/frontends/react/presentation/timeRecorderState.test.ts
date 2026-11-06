import { describe, it } from 'vitest'
import {
  initialTimeRecorderState,
  timeRecorderReducer,
  type TimeRecorderState,
} from '../../../../src/frontends/react/src/presentation/timeRecorderState'
import { presentedRecord, timestamp } from '../test/timeRecords'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 記録 = presentedRecord(記録ID, 開始時刻)
const 終了済み記録 = presentedRecord(記録ID, 開始時刻, 終了時刻)

describe('timeRecorderReducer', () => {
  it('記録中の作業記録一覧を取得すると一覧と現在記録を保持します', () => {
    const result = timeRecorderReducer(initialTimeRecorderState, {
      type: 'recordsLoaded',
      records: [記録],
    })

    result.should.deep.equal({
      ...initialTimeRecorderState,
      records: [記録],
      timeRecordId: 記録ID,
    })
  })

  it('終了済みの作業記録一覧を取得すると現在記録を解除します', () => {
    const state: TimeRecorderState = {
      ...initialTimeRecorderState,
      timeRecordId: 記録ID,
    }

    const result = timeRecorderReducer(state, {
      type: 'recordsLoaded',
      records: [終了済み記録],
    })

    result.should.have.property('timeRecordId', null)
  })

  it('記録を開始すると一覧と現在の記録を更新して終了時刻を消去します', () => {
    const state: TimeRecorderState = {
      ...initialTimeRecorderState,
      endedAt: 終了時刻,
    }

    const result = timeRecorderReducer(state, {
      type: 'recordStarted',
      record: 記録,
    })

    result.should.deep.equal({
      records: [記録],
      timeRecordId: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
    })
  })

  it('記録を終了すると一覧を更新して現在の記録を解除し終了時刻を保持します', () => {
    const state: TimeRecorderState = {
      records: [記録],
      timeRecordId: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
    }

    const result = timeRecorderReducer(state, {
      type: 'recordEnded',
      record: 終了済み記録,
    })

    result.should.deep.equal({
      records: [終了済み記録],
      timeRecordId: null,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    })
  })
})
