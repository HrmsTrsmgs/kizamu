import { Project } from 'kizamu-domain-typescript'
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
  it('プロジェクトを切り替えて戻ると元の記録中状態を保持します', () => {
    const プロジェクトA = new Project('プロジェクトA')
    const プロジェクトB = new Project('プロジェクトB')
    let tested = timeRecorderReducer(initialTimeRecorderState, {
      type: 'projectSelected',
      project: プロジェクトA,
    })
    tested = timeRecorderReducer(tested, {
      type: 'recordStarted',
      record: 記録,
    })

    tested = timeRecorderReducer(tested, {
      type: 'projectSelected',
      project: プロジェクトB,
    })

    tested.should.have.property('timeRecordId', null)

    tested = timeRecorderReducer(tested, {
      type: 'projectSelected',
      project: プロジェクトA,
    })

    tested.should.have.property('timeRecordId', 記録ID)
    tested.records.should.deep.equal([記録])
  })

  it('未指定とプロジェクトを切り替えて戻ると元の記録中状態を保持します', () => {
    const プロジェクトA = new Project('プロジェクトA')
    let tested = timeRecorderReducer(initialTimeRecorderState, {
      type: 'recordStarted',
      record: 記録,
    })

    tested = timeRecorderReducer(tested, {
      type: 'projectSelected',
      project: プロジェクトA,
    })

    tested.should.have.property('timeRecordId', null)

    tested = timeRecorderReducer(tested, {
      type: 'projectSelected',
      project: undefined,
    })

    tested.should.have.property('timeRecordId', 記録ID)
    tested.records.should.deep.equal([記録])
  })

  it('プロジェクトの記録を終了しても未指定の記録は記録中のままです', () => {
    const プロジェクトA = new Project('プロジェクトA')
    const プロジェクトの記録ID = '019cdeac-b600-7000-8000-000000000002'
    const プロジェクトの記録 = presentedRecord(プロジェクトの記録ID, 開始時刻)
    const 終了済みのプロジェクトの記録 = presentedRecord(プロジェクトの記録ID, 開始時刻, 終了時刻)
    let tested = 未指定とプロジェクトの記録を開始する(プロジェクトA, プロジェクトの記録)

    tested = timeRecorderReducer(tested, { type: 'recordEnded', record: 終了済みのプロジェクトの記録 })
    tested = timeRecorderReducer(tested, { type: 'projectSelected', project: undefined })

    tested.should.have.property('timeRecordId', 記録ID)
    tested.records.should.deep.equal([記録, 終了済みのプロジェクトの記録])
  })

  it('未指定の記録を終了してもプロジェクトの記録は記録中のままです', () => {
    const プロジェクトA = new Project('プロジェクトA')
    const プロジェクトの記録ID = '019cdeac-b600-7000-8000-000000000002'
    const プロジェクトの記録 = presentedRecord(プロジェクトの記録ID, 開始時刻)
    let tested = 未指定とプロジェクトの記録を開始する(プロジェクトA, プロジェクトの記録)

    tested = timeRecorderReducer(tested, { type: 'projectSelected', project: undefined })
    tested = timeRecorderReducer(tested, { type: 'recordEnded', record: 終了済み記録 })
    tested = timeRecorderReducer(tested, { type: 'projectSelected', project: プロジェクトA })

    tested.should.have.property('timeRecordId', プロジェクトの記録ID)
    tested.records.should.deep.equal([終了済み記録, プロジェクトの記録])
  })

  it('記録中の作業記録一覧を取得すると一覧と現在記録を保持します', () => {
    const result = timeRecorderReducer(initialTimeRecorderState, {
      type: 'recordsLoaded',
      records: [記録],
    })

    result.should.toStrictEqual({
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

    result.should.toStrictEqual({
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

    result.should.toStrictEqual({
      records: [終了済み記録],
      timeRecordId: null,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    })
  })
})

function 未指定とプロジェクトの記録を開始する(project: Project, projectRecord: typeof 記録) {
  let state = timeRecorderReducer(initialTimeRecorderState, { type: 'recordStarted', record: 記録 })
  state = timeRecorderReducer(state, { type: 'projectSelected', project })
  return timeRecorderReducer(state, { type: 'recordStarted', record: projectRecord })
}
