// @vitest-environment node
import { Project } from 'kizamu-domain-typescript'
import { beforeEach, describe, it } from 'vitest'
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
  describe('プロジェクトの切り替え', () => {
    let プロジェクトA: Project

    beforeEach(() => {
      プロジェクトA = new Project('プロジェクトA')
    })

    it('プロジェクトを切り替えて戻ると元の記録中状態を保持します', () => {
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

    it('AとBで順に開始してAに戻って終了すると操作欄にAの開始と終了を表示します', () => {
      const プロジェクトB = new Project('プロジェクトB')
      const Bの記録 = presentedRecord('Bの記録', timestamp('2026-08-01T10:00:00Z'))
      let state = timeRecorderReducer(initialTimeRecorderState, { type: 'projectSelected', project: プロジェクトA })
      state = timeRecorderReducer(state, { type: 'recordStarted', record: 記録 })
      state = timeRecorderReducer(state, { type: 'projectSelected', project: プロジェクトB })
      state = timeRecorderReducer(state, { type: 'recordStarted', record: Bの記録 })
      state = timeRecorderReducer(state, { type: 'projectSelected', project: プロジェクトA })

      state = timeRecorderReducer(state, { type: 'recordEnded', record: 終了済み記録 })

      state.should.have.property('displayedRecord', 終了済み記録)
    })

    it('未指定とプロジェクトを切り替えて戻ると元の記録中状態を保持します', () => {
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

    describe('未指定とプロジェクトAの両方で記録中のとき', () => {
      const プロジェクトの記録ID = '019cdeac-b600-7000-8000-000000000002'
      let プロジェクトの記録: typeof 記録
      let tested: TimeRecorderState

      beforeEach(() => {
        プロジェクトの記録 = presentedRecord(プロジェクトの記録ID, 開始時刻)
        tested = timeRecorderReducer(initialTimeRecorderState, { type: 'recordStarted', record: 記録 })
        tested = timeRecorderReducer(tested, { type: 'projectSelected', project: プロジェクトA })
        tested = timeRecorderReducer(tested, { type: 'recordStarted', record: プロジェクトの記録 })
      })

      it('プロジェクトの記録を終了しても未指定の記録は記録中のままです', () => {
        const 終了済みのプロジェクトの記録 = presentedRecord(プロジェクトの記録ID, 開始時刻, 終了時刻)

        tested = timeRecorderReducer(tested, { type: 'recordEnded', record: 終了済みのプロジェクトの記録 })
        tested = timeRecorderReducer(tested, { type: 'projectSelected', project: undefined })

        tested.should.have.property('timeRecordId', 記録ID)
        tested.records.should.deep.equal([記録, 終了済みのプロジェクトの記録])
      })

      it('未指定の記録を終了してもプロジェクトの記録は記録中のままです', () => {
        tested = timeRecorderReducer(tested, { type: 'projectSelected', project: undefined })
        tested = timeRecorderReducer(tested, { type: 'recordEnded', record: 終了済み記録 })
        tested = timeRecorderReducer(tested, { type: 'projectSelected', project: プロジェクトA })

        tested.should.have.property('timeRecordId', プロジェクトの記録ID)
        tested.records.should.deep.equal([終了済み記録, プロジェクトの記録])
      })
    })
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

  describe('一覧取得前に終了対象があるとき', () => {
    let 取得前の状態: TimeRecorderState

    beforeEach(() => {
      取得前の状態 = { ...initialTimeRecorderState, timeRecordId: 記録ID }
    })

    it('終了済みの作業記録一覧を取得すると現在記録を解除します', () => {
      const result = timeRecorderReducer(取得前の状態, {
        type: 'recordsLoaded',
        records: [終了済み記録],
      })

      result.should.have.property('timeRecordId', null)
    })

    it('直前の終了対象が取得中に記録中だった場合、一覧には最後の更新を使い終了対象にはそのIDを保持します', () => {
      const 別の記録中の記録 = presentedRecord('別の記録', 開始時刻)

      const state = timeRecorderReducer(取得前の状態, {
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

      const state = timeRecorderReducer(取得前の状態, {
        type: 'recordsLoaded',
        records: [記録, 先の記録, 次の記録],
        updatedRecords: [終了済み記録],
      })

      state.should.have.property('timeRecordId', 先の記録.record.id)
    })
  })

  describe('取得した一覧に同じIDの記録が複数あるとき', () => {
    const 変更しない記録 = presentedRecord('保存済み', 開始時刻, 終了時刻)
    const 別表記の開始時刻 = timestamp('2026-08-01T18:00:00.0000000+09:00')
    const 最後の更新 = presentedRecord(記録ID, 別表記の開始時刻, 終了時刻)

    it('一覧の各位置を保ち同じIDの記録だけを最後に受け取った記録で置き換えます', () => {
      const state = timeRecorderReducer(initialTimeRecorderState, {
        type: 'recordsLoaded',
        records: [記録, 変更しない記録, 記録],
        updatedRecords: [記録, 最後の更新],
      })

      state.records.should.toStrictEqual([最後の更新, 変更しない記録, 最後の更新])
      state.records[0].should.equal(最後の更新)
      state.records[1].should.equal(変更しない記録)
      state.records[2].should.equal(最後の更新)
    })

    it('置き換えた記録の開始時刻は最後の更新のISO表記を保持します', () => {
      const state = timeRecorderReducer(initialTimeRecorderState, {
        type: 'recordsLoaded',
        records: [記録, 変更しない記録, 記録],
        updatedRecords: [記録, 最後の更新],
      })

      state.records[0].isoText.startedAt.should.equal(別表記の開始時刻.isoText)
    })
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

  it('記録を開始すると一覧と現在の記録を更新して終了時刻を消去します', () => {
    const state: TimeRecorderState = {
      ...initialTimeRecorderState,
      displayedRecord: 終了済み記録,
    }

    const result = timeRecorderReducer(state, {
      type: 'recordStarted',
      record: 記録,
    })

    result.should.toStrictEqual({
      records: [記録],
      timeRecordId: 記録ID,
      displayedRecord: 記録,
    })
  })

  it('再び開始すると前回の記録を残して終了時刻を消去します', () => {
    const 次の開始時刻 = timestamp('2026-08-01T11:00:00Z')
    const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
    const 次の記録 = presentedRecord(次の記録ID, 次の開始時刻)
    const 前回を終了した状態 = {
      ...initialTimeRecorderState,
      records: [終了済み記録],
      displayedRecord: 終了済み記録,
    }

    const state = timeRecorderReducer(前回を終了した状態, { type: 'recordStarted', record: 次の記録 })

    state.should.toStrictEqual({
      records: [終了済み記録, 次の記録],
      timeRecordId: 次の記録ID,
      displayedRecord: 次の記録,
    })
  })

  describe('別のプロジェクトを表示中に未指定で開始した記録の応答が届くとき', () => {
    const 表示中の状態 = {
      ...initialTimeRecorderState,
      selectedProject: new Project('プロジェクトA'),
      records: [終了済み記録],
      displayedRecord: 終了済み記録,
    }
    const 未指定の記録 = presentedRecord('未指定の記録', timestamp('2026-08-01T11:00:00Z'))

    it('共有の一覧に未指定の記録を追加します', () => {
      const state = timeRecorderReducer(表示中の状態, { type: 'recordStarted', project: undefined, record: 未指定の記録 })

      state.records.should.toStrictEqual([終了済み記録, 未指定の記録])
    })

    it('表示中の時刻を保ち終了対象を開始元の未指定に割り当てます', () => {
      const state = timeRecorderReducer(表示中の状態, { type: 'recordStarted', project: undefined, record: 未指定の記録 })

      state.should.have.property('selectedProject', 表示中の状態.selectedProject)
      state.should.have.property('timeRecordId', null)
      state.should.have.property('displayedRecord', 終了済み記録)

      const 未指定の状態 = timeRecorderReducer(state, { type: 'projectSelected', project: undefined })
      未指定の状態.should.have.property('timeRecordId', '未指定の記録')
    })
  })

  it('記録を終了すると一覧を更新して現在の記録を解除し終了時刻を保持します', () => {
    const state: TimeRecorderState = {
      records: [記録],
      timeRecordId: 記録ID,
      displayedRecord: 記録,
    }

    const result = timeRecorderReducer(state, {
      type: 'recordEnded',
      record: 終了済み記録,
    })

    result.should.toStrictEqual({
      records: [終了済み記録],
      timeRecordId: null,
      displayedRecord: 終了済み記録,
    })
  })
})
