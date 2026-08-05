import { describe, expect, it } from 'vitest'
import {
  initialTimeRecorderState,
  timeRecorderReducer,
  type TimeRecorderState,
} from './timeRecorderState'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'
const 記録 = { id: 記録ID, startedAt: 開始時刻, endedAt: null }

describe('timeRecorderReducer', () => {
  it('作業記録一覧を取得すると一覧を保持します', () => {
    const result = timeRecorderReducer(initialTimeRecorderState, {
      type: 'recordsLoaded',
      records: [記録],
    })

    expect(result).toEqual({
      ...initialTimeRecorderState,
      records: [記録],
    })
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

    expect(result).toEqual({
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
      record: { ...記録, endedAt: 終了時刻 },
    })

    expect(result).toEqual({
      records: [{ ...記録, endedAt: 終了時刻 }],
      timeRecordId: null,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    })
  })
})
