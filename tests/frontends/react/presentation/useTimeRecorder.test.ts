import { act, renderHook, waitFor } from '@testing-library/react'
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
    const getTimeRecords = vi.fn().mockResolvedValueOnce([]).mockResolvedValueOnce([記録])
    const service = createTimeRecordServiceStub({
      getTimeRecords, startTimeRecord: async () => { throw new Error('failed') },
    })
    const { result } = renderHook(() => useTimeRecorder(service))
    await waitFor(() => getTimeRecords.should.have.callCount(1))

    await act(async () => { await expect(result.current.startTimeRecord()).rejects.toThrow('failed') })

    getTimeRecords.should.have.callCount(2)
    result.current.state.records.should.deep.equal([記録])
    result.current.state.should.have.property('timeRecordId', 記録ID)
  })

  it('終了の通信に失敗すると一覧を再取得して反映します', async () => {
    const getTimeRecords = vi.fn().mockResolvedValueOnce([記録]).mockResolvedValueOnce([終了済み記録])
    const service = createTimeRecordServiceStub({
      getTimeRecords, endTimeRecord: async () => { throw new Error('failed') },
    })
    const { result } = renderHook(() => useTimeRecorder(service))
    await waitFor(() => result.current.state.should.have.property('timeRecordId', 記録ID))

    await act(async () => { await expect(result.current.endTimeRecord()).rejects.toThrow('failed') })

    getTimeRecords.should.have.callCount(2)
    result.current.state.records.should.deep.equal([終了済み記録])
    result.current.state.should.have.property('timeRecordId', null)
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
})


function 開始応答を保留する() {
  const response = deferred<typeof 記録>()
  const startTimeRecord = vi.fn().mockReturnValueOnce(response.promise).mockResolvedValue(記録)
  const endTimeRecord = vi.fn(async () => 終了済み記録)
  const service = createTimeRecordServiceStub({ startTimeRecord, endTimeRecord })
  const { result } = renderHook(() => useTimeRecorder(service))
  return { response, startTimeRecord, endTimeRecord, result }
}

function 終了応答を保留する() {
  const response = deferred<typeof 終了済み記録>()
  const startTimeRecord = vi.fn(async () => 記録)
  const endTimeRecord = vi.fn().mockReturnValueOnce(response.promise).mockResolvedValue(終了済み記録)
  const getTimeRecords = vi.fn().mockResolvedValueOnce([]).mockResolvedValue([記録])
  const service = createTimeRecordServiceStub({ getTimeRecords, startTimeRecord, endTimeRecord })
  const { result } = renderHook(() => useTimeRecorder(service))
  return { response, startTimeRecord, endTimeRecord, result }
}
