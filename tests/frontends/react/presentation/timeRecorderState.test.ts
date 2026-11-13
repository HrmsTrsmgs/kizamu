// @vitest-environment node
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

  it('取得した一覧の各位置を保ち、同じIDの最後の更新と元のISO表記を使います', () => {
    const 変更しない記録 = presentedRecord('保存済み', 開始時刻, 終了時刻)
    const 別表記の開始時刻 = timestamp('2026-08-01T18:00:00.0000000+09:00')
    const 最後の更新 = presentedRecord(記録ID, 別表記の開始時刻, 終了時刻)

    const state = timeRecorderReducer(initialTimeRecorderState, {
      type: 'recordsLoaded',
      records: [記録, 変更しない記録, 記録],
      updatedRecords: [記録, 最後の更新],
    })

    state.records.should.toStrictEqual([最後の更新, 変更しない記録, 最後の更新])
    state.records[0].should.equal(最後の更新)
    state.records[1].should.equal(変更しない記録)
    state.records[2].should.equal(最後の更新)
    state.records[0].isoText.startedAt.should.equal(別表記の開始時刻.isoText)
  })

  it('取得した一覧にない記録は各IDの最後の更新順に一度ずつ追加します', () => {
    const 過去の記録 = presentedRecord('保存済み', 開始時刻, 終了時刻)
    const 別の記録 = presentedRecord('別の記録', 開始時刻)
    const 次の記録 = presentedRecord('次の記録', 開始時刻)

    const state = timeRecorderReducer(initialTimeRecorderState, {
      type: 'recordsLoaded',
      records: [過去の記録],
      updatedRecords: [記録, 別の記録, 記録, 次の記録, 終了済み記録, 終了済み記録],
    })

    state.records.should.toStrictEqual([過去の記録, 別の記録, 次の記録, 終了済み記録])
    state.records[3].should.equal(終了済み記録)
  })

  it('直前の終了対象が取得中に記録中だった場合、一覧には最後の更新を使い終了対象にはそのIDを保持します', () => {
    const 別の記録中の記録 = presentedRecord('別の記録', 開始時刻)

    const state = timeRecorderReducer({ ...initialTimeRecorderState, timeRecordId: 記録ID }, {
      type: 'recordsLoaded',
      records: [別の記録中の記録, 記録],
      updatedRecords: [記録, 終了済み記録],
    })

    state.records.should.toStrictEqual([別の記録中の記録, 終了済み記録])
    state.should.have.property('timeRecordId', 記録ID)
  })

  it('直前の終了対象に記録中の更新がなければ、合成した一覧で最初の記録中のIDを使います', () => {
    const 先の記録 = presentedRecord('先の記録', 開始時刻)
    const 次の記録 = presentedRecord('次の記録', 開始時刻)

    const state = timeRecorderReducer({ ...initialTimeRecorderState, timeRecordId: 記録ID }, {
      type: 'recordsLoaded',
      records: [記録, 先の記録, 次の記録],
      updatedRecords: [終了済み記録],
    })

    state.should.have.property('timeRecordId', 先の記録.record.id)
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

  it('再び開始すると前回の記録を残して終了時刻を消去します', () => {
    const 次の開始時刻 = timestamp('2026-08-01T11:00:00Z')
    const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
    const 次の記録 = presentedRecord(次の記録ID, 次の開始時刻)
    const 前回を終了した状態 = {
      ...initialTimeRecorderState,
      records: [終了済み記録],
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }

    const state = timeRecorderReducer(前回を終了した状態, { type: 'recordStarted', record: 次の記録 })

    state.should.toStrictEqual({
      records: [終了済み記録, 次の記録],
      timeRecordId: 次の記録ID,
      startedAt: 次の開始時刻,
      endedAt: null,
    })
  })

  it('未指定で開始した応答を別のプロジェクトで受け取ると、その表示時刻と終了対象を保ちます', () => {
    const 表示中の状態 = {
      ...initialTimeRecorderState,
      selectedProject: new Project('プロジェクトA'),
      records: [終了済み記録],
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }
    const 未指定の記録 = presentedRecord('未指定の記録', timestamp('2026-08-01T11:00:00Z'))

    const state = timeRecorderReducer(表示中の状態, { type: 'recordStarted', project: undefined, record: 未指定の記録 })

    state.records.should.toStrictEqual([終了済み記録, 未指定の記録])
    state.should.have.property('selectedProject', 表示中の状態.selectedProject)
    state.should.have.property('timeRecordId', null)
    state.should.have.property('startedAt', 開始時刻)
    state.should.have.property('endedAt', 終了時刻)

    const 未指定の状態 = timeRecorderReducer(state, { type: 'projectSelected', project: undefined })
    未指定の状態.should.have.property('timeRecordId', '未指定の記録')
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
