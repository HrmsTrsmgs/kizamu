import { act, renderHook, waitFor } from '@testing-library/react'
import { Project } from 'kizamu-domain-typescript'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Mock } from 'vitest'
import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'
import { useTimeRecorder } from '../../../../src/frontends/react/src/presentation/useTimeRecorder'
import { createTimeRecordServiceStub } from '../test/createTimeRecordServiceStub'
import { deferred } from '../test/deferred'
import { presentedRecord, timestamp } from '../test/timeRecords'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
const 次の開始時刻 = timestamp('2026-08-01T11:00:00Z')
const 記録 = presentedRecord(記録ID, 開始時刻)
const 終了済み記録 = presentedRecord(記録ID, 開始時刻, 終了時刻)

describe('useTimeRecorder', () => {
  let getTimeRecords: Mock<TimeRecordService['getTimeRecords']>
  let startTimeRecord: Mock<TimeRecordService['startTimeRecord']>
  let endTimeRecord: Mock<TimeRecordService['endTimeRecord']>
  let service: TimeRecordService

  beforeEach(() => {
    getTimeRecords = vi.fn(async () => [])
    startTimeRecord = vi.fn(async () => 記録)
    endTimeRecord = vi.fn(async () => 終了済み記録)
    service = createTimeRecordServiceStub({ getTimeRecords, startTimeRecord, endTimeRecord })
  })

  it('開始の通信中は追加操作を受け付けず成功後に終了できます', async () => {
    const { response, startTimeRecord, endTimeRecord, result } = 開始応答を保留する()
    let operation = Promise.resolve()
    act(() => { operation = result.current.startTimeRecord(); void result.current.startTimeRecord(); void result.current.endTimeRecord() })
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
    await act(async () => { response.resolve(記録); await operation })
    await act(() => result.current.endTimeRecord())
    endTimeRecord.should.have.callCount(1)
  })

  it('開始の通信中は追加操作を受け付けず失敗後に再開始できます', async () => {
    const { response, startTimeRecord, endTimeRecord, result } = 開始応答を保留する()
    let operation = Promise.resolve()
    act(() => { operation = result.current.startTimeRecord(); void result.current.startTimeRecord(); void result.current.endTimeRecord() })
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
    await act(async () => { response.reject(new Error('failed')); await expect(operation).rejects.toThrow('failed') })
    await act(() => result.current.startTimeRecord())
    startTimeRecord.should.have.callCount(2)
  })

  it('終了の通信中は追加操作を受け付けず成功後に開始できます', async () => {
    const { response, startTimeRecord, endTimeRecord, result } = 終了応答を保留する()
    await act(() => result.current.startTimeRecord())
    let operation = Promise.resolve()
    act(() => { operation = result.current.endTimeRecord(); void result.current.startTimeRecord(); void result.current.endTimeRecord() })
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(1)
    await act(async () => { response.resolve(終了済み記録); await operation })
    await act(() => result.current.startTimeRecord())
    startTimeRecord.should.have.callCount(2)
  })

  it('終了の通信中は追加操作を受け付けず失敗後に再終了できます', async () => {
    const { response, startTimeRecord, endTimeRecord, result } = 終了応答を保留する()
    await act(() => result.current.startTimeRecord())
    let operation = Promise.resolve()
    act(() => { operation = result.current.endTimeRecord(); void result.current.startTimeRecord(); void result.current.endTimeRecord() })
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(1)
    await act(async () => { response.reject(new Error('failed')); await expect(operation).rejects.toThrow('failed') })
    await act(() => result.current.endTimeRecord())
    endTimeRecord.should.have.callCount(2)
  })

  it('開始の通信に失敗すると一覧を再取得して反映します', async () => {
    const 開始失敗 = new Error('failed')
    getTimeRecords.mockResolvedValueOnce([]).mockResolvedValueOnce([記録])
    startTimeRecord.mockRejectedValue(開始失敗)
    const { result } = renderRecorder()
    await waitFor(() => getTimeRecords.should.have.callCount(1))

    await act(async () => { await expect(result.current.startTimeRecord()).rejects.toBe(開始失敗) })

    getTimeRecords.should.have.callCount(2)
    result.current.state.records.should.deep.equal([記録])
    result.current.state.should.have.property('timeRecordId', 記録ID)
  })

  it('終了の通信に失敗すると一覧を再取得して反映します', async () => {
    const 終了失敗 = new Error('failed')
    getTimeRecords.mockResolvedValueOnce([記録]).mockResolvedValueOnce([終了済み記録])
    endTimeRecord.mockRejectedValue(終了失敗)
    const { result } = renderRecorder()
    await waitFor(() => result.current.state.should.have.property('timeRecordId', 記録ID))

    await act(async () => { await expect(result.current.endTimeRecord()).rejects.toBe(終了失敗) })

    getTimeRecords.should.have.callCount(2)
    result.current.state.records.should.deep.equal([終了済み記録])
    result.current.state.should.have.property('timeRecordId', null)
  })

  it('開始失敗後の一覧再取得にも失敗すると直前の一覧を残して未確認状態になります', async () => {
    const { result } = await 終了済みの一覧を読み込む()
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    const 再取得失敗 = new Error('一覧取得失敗')
    getTimeRecords.mockRejectedValue(再取得失敗)

    await act(async () => {
      await expect(result.current.startTimeRecord()).rejects.toBe(再取得失敗)
    })

    result.current.state.records.should.toStrictEqual([終了済み記録])
    result.current.should.have.property('isStateUnconfirmed', true)
    result.current.should.have.property('isBusy', false)
  })

  it('開始失敗後に古い初回一覧が届いても未確認状態と操作の抑止を保ちます', async () => {
    const 初期一覧の応答 = deferred<(typeof 記録)[]>()
    getTimeRecords.mockReturnValueOnce(初期一覧の応答.promise).mockRejectedValueOnce(new Error('一覧取得失敗'))
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    const { result } = renderRecorder()

    await act(async () => { await expect(result.current.startTimeRecord()).rejects.toThrow('一覧取得失敗') })
    await act(async () => { 初期一覧の応答.resolve([]); await 初期一覧の応答.promise })
    await act(() => result.current.startTimeRecord())
    await act(() => result.current.endTimeRecord())

    result.current.should.have.property('isStateUnconfirmed', true)
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
  })

  it('終了失敗後の一覧再取得にも失敗すると直前の一覧を残して未確認状態になります', async () => {
    const { result } = await 記録中の一覧を読み込む()
    endTimeRecord.mockRejectedValueOnce(new Error('終了失敗'))
    const 再取得失敗 = new Error('一覧取得失敗')
    getTimeRecords.mockRejectedValue(再取得失敗)

    await act(async () => {
      await expect(result.current.endTimeRecord()).rejects.toBe(再取得失敗)
    })

    result.current.state.records.should.toStrictEqual([記録])
    result.current.should.have.property('isStateUnconfirmed', true)
    result.current.should.have.property('isBusy', false)
  })

  it('開始失敗後の状態を確認できるまでは追加の開始と終了を要求しません', async () => {
    const { result } = await 開始失敗後の再取得にも失敗させる()

    await act(() => result.current.startTimeRecord())
    await act(() => result.current.endTimeRecord())

    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
    getTimeRecords.should.have.callCount(2)
  })

  it('終了失敗後の状態を確認できるまでは追加の終了と開始を要求しません', async () => {
    const { result } = await 終了失敗後の再取得にも失敗させる()

    await act(() => result.current.endTimeRecord())
    await act(() => result.current.startTimeRecord())

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    startTimeRecord.should.have.callCount(0)
    getTimeRecords.should.have.callCount(2)
  })

  it('手動再取得に成功すると取得した記録を終了対象として操作を再開できます', async () => {
    const { result } = await 開始失敗後の再取得にも失敗させる()
    getTimeRecords.mockResolvedValue([記録])

    await act(() => result.current.reloadTimeRecords())

    result.current.state.records.should.toStrictEqual([記録])
    result.current.should.have.property('isStateUnconfirmed', false)
    result.current.state.should.have.property('timeRecordId', 記録ID)
    await act(() => result.current.endTimeRecord())
    endTimeRecord.should.have.been.calledOnceWith(記録ID)
  })

  it('手動再取得で終了済みの一覧を確認すると開始できます', async () => {
    const { result } = await 終了失敗後の再取得にも失敗させる()
    getTimeRecords.mockResolvedValue([終了済み記録])

    await act(() => result.current.reloadTimeRecords())

    result.current.state.records.should.toStrictEqual([終了済み記録])
    result.current.should.have.property('isStateUnconfirmed', false)
    result.current.state.should.have.property('timeRecordId', null)
    await act(() => result.current.startTimeRecord())
    startTimeRecord.should.have.callCount(1)
  })

  it('手動再取得にも失敗すると直前の一覧と未確認状態を保ちます', async () => {
    const { result } = await 開始失敗後の再取得にも失敗させる()

    await act(async () => {
      await expect(result.current.reloadTimeRecords()).rejects.toThrow('一覧取得失敗')
    })

    result.current.state.records.should.toStrictEqual([終了済み記録])
    result.current.should.have.property('isStateUnconfirmed', true)
    result.current.should.have.property('isBusy', false)
    getTimeRecords.should.have.callCount(3)
  })

  it('手動再取得の通信中は再取得と開始と終了の追加要求を送りません', async () => {
    const { result } = await 開始失敗後の再取得にも失敗させる()
    const response = deferred<(typeof 記録)[]>()
    getTimeRecords.mockReturnValue(response.promise)
    let 再取得 = Promise.resolve()
    let 重複再取得 = Promise.resolve()

    act(() => {
      再取得 = result.current.reloadTimeRecords()
      重複再取得 = result.current.reloadTimeRecords()
    })
    const 再取得中 = result.current.isBusy
    await act(() => result.current.startTimeRecord())
    await act(() => result.current.endTimeRecord())
    await act(async () => {
      response.resolve([終了済み記録])
      await Promise.all([再取得, 重複再取得])
    })

    再取得中.should.equal(true)
    getTimeRecords.should.have.callCount(3)
    startTimeRecord.should.have.callCount(1)
    endTimeRecord.should.have.callCount(0)
  })

  it('初期化で一覧を一度要求し取得した記録と終了対象を公開します', async () => {
    const 一覧の記録中の記録 = presentedRecord(次の記録ID, 開始時刻)
    getTimeRecords.mockResolvedValue([終了済み記録, 一覧の記録中の記録])

    const { result } = renderRecorder()

    await waitFor(() => result.current.state.records.should.have.lengthOf(2))
    getTimeRecords.should.have.callCount(1)
    result.current.state.records.should.toStrictEqual([終了済み記録, 一覧の記録中の記録])
    result.current.state.should.have.property('timeRecordId', 次の記録ID)
  })

  it('開始操作でサービスを呼び出して確定した開始時刻と記録を保持します', async () => {
    const { result } = renderRecorder()

    await act(() => result.current.startTimeRecord())

    startTimeRecord.should.have.callCount(1)
    result.current.state.should.toStrictEqual({
      records: [記録], timeRecordId: 記録ID, startedAt: 開始時刻, endedAt: null,
    })
  })

  it('開始の応答前に対象を切り替えても開始元の記録中状態を更新します', async () => {
    const プロジェクトA = new Project('プロジェクトA')
    const 応答 = deferred<typeof 記録>()
    startTimeRecord.mockReturnValue(応答.promise)
    const { result } = renderRecorder()
    await waitFor(() => getTimeRecords.should.have.callCount(1))

    act(() => result.current.selectProject(プロジェクトA))
    let 開始操作 = Promise.resolve()
    act(() => { 開始操作 = result.current.startTimeRecord() })
    act(() => result.current.selectProject(undefined))
    await act(async () => {
      応答.resolve(記録)
      await 開始操作
    })

    result.current.state.records.should.deep.equal([記録])
    result.current.state.should.have.property('timeRecordId', null)
    act(() => result.current.selectProject(プロジェクトA))
    result.current.state.should.have.property('timeRecordId', 記録ID)
  })
  it('開始後に古い一覧が届いても過去の記録と開始した記録が残り終了できます', async () => {
    const 過去の記録 = presentedRecord(次の記録ID, 開始時刻, 終了時刻)
    const initialListResponse = deferred<(typeof 記録)[]>()
    getTimeRecords.mockReturnValue(initialListResponse.promise)
    const { result } = renderRecorder()

    await act(() => result.current.startTimeRecord())
    await act(async () => {
      initialListResponse.resolve([過去の記録])
      await initialListResponse.promise
    })

    result.current.state.should.toStrictEqual({
      records: [過去の記録, 記録],
      timeRecordId: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
    })

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    result.current.state.should.toStrictEqual({
      records: [過去の記録, 終了済み記録],
      timeRecordId: null,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    })
  })

  it('操作後の再読み込みはサーバーの更新と削除を反映します', async () => {
    const 過去の記録 = presentedRecord(次の記録ID, 開始時刻, 終了時刻)
    getTimeRecords.mockResolvedValue([過去の記録])
    const { result, rerender } = renderRecorder()
    await waitFor(() => result.current.state.records.should.toStrictEqual([過去の記録]))
    await act(() => result.current.startTimeRecord())

    rerender({
      service: { ...service, getTimeRecords: async () => [終了済み記録] },
    })

    await waitFor(() => result.current.state.records.should.toStrictEqual([終了済み記録]))
    result.current.state.should.have.property('timeRecordId', null)
  })

  it('終了操作で現在の記録を指定して確定した終了時刻と一覧を更新します', async () => {
    const { result } = await renderStartedRecorder()

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    result.current.state.should.toStrictEqual({
      records: [終了済み記録], timeRecordId: null, startedAt: 開始時刻, endedAt: 終了時刻,
    })
  })

  it('再び開始すると前回の記録を残して終了時刻を消去します', async () => {
    const 次の記録 = presentedRecord(次の記録ID, 次の開始時刻)
    const { result } = await renderEndedRecorder(次の記録)

    await act(() => result.current.startTimeRecord())

    result.current.state.should.toStrictEqual({
      records: [終了済み記録, 次の記録], timeRecordId: 次の記録ID, startedAt: 次の開始時刻, endedAt: null,
    })
  })

  it('記録中でなければ終了を要求しません', async () => {
    const { result } = renderRecorder()

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.callCount(0)
  })

  async function 終了済みの一覧を読み込む() {
    getTimeRecords.mockResolvedValueOnce([終了済み記録])
    const recorder = renderRecorder()
    await waitFor(() => recorder.result.current.state.records.should.toStrictEqual([終了済み記録]))
    return recorder
  }

  async function 記録中の一覧を読み込む() {
    getTimeRecords.mockResolvedValueOnce([記録])
    const recorder = renderRecorder()
    await waitFor(() => recorder.result.current.state.should.have.property('timeRecordId', 記録ID))
    return recorder
  }

  async function 開始失敗後の再取得にも失敗させる() {
    const recorder = await 終了済みの一覧を読み込む()
    startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
    getTimeRecords.mockRejectedValue(new Error('一覧取得失敗'))
    await act(async () => {
      await expect(recorder.result.current.startTimeRecord()).rejects.toThrow()
    })
    return recorder
  }

  async function 終了失敗後の再取得にも失敗させる() {
    const recorder = await 記録中の一覧を読み込む()
    endTimeRecord.mockRejectedValueOnce(new Error('終了失敗'))
    getTimeRecords.mockRejectedValue(new Error('一覧取得失敗'))
    await act(async () => {
      await expect(recorder.result.current.endTimeRecord()).rejects.toThrow()
    })
    return recorder
  }

  function renderRecorder() {
    return renderHook(({ service }) => useTimeRecorder(service), { initialProps: { service } })
  }

  async function renderStartedRecorder() {
    const recorder = renderRecorder()
    await act(() => recorder.result.current.startTimeRecord())

    return recorder
  }

  async function renderEndedRecorder(nextRecord: typeof 記録) {
    startTimeRecord.mockResolvedValueOnce(記録).mockResolvedValueOnce(nextRecord)
    const recorder = await renderStartedRecorder()
    await act(() => recorder.result.current.endTimeRecord())

    return recorder
  }

  function 開始応答を保留する() {
    const response = deferred<typeof 記録>()
    startTimeRecord.mockReturnValueOnce(response.promise)
    const { result } = renderRecorder()
    return { response, startTimeRecord, endTimeRecord, result }
  }

  function 終了応答を保留する() {
    const response = deferred<typeof 終了済み記録>()
    endTimeRecord.mockReturnValueOnce(response.promise)
    getTimeRecords.mockResolvedValueOnce([]).mockResolvedValue([記録])
    const { result } = renderRecorder()
    return { response, startTimeRecord, endTimeRecord, result }
  }
})
