import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, it, vi } from 'vitest'
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
  it('初期化で一覧を一度要求し取得した記録と終了対象を公開します', async () => {
    const 一覧の記録中の記録 = presentedRecord(次の記録ID, 開始時刻)
    const getTimeRecords = vi.fn(async () => [終了済み記録, 一覧の記録中の記録])
    const service = createTimeRecordServiceStub({ getTimeRecords })

    const { result } = renderHook(() => useTimeRecorder(service))

    await waitFor(() => result.current.state.records.should.have.lengthOf(2))
    getTimeRecords.should.have.callCount(1)
    result.current.state.records.should.deep.equal([終了済み記録, 一覧の記録中の記録])
    result.current.state.should.have.property('timeRecordId', 次の記録ID)
  })

  it('開始操作でサービスを呼び出して確定した開始時刻と記録を保持します', async () => {
    const startTimeRecord = vi.fn(async () => 記録)
    const service = createTimeRecordServiceStub({ startTimeRecord })
    const { result } = renderHook(() => useTimeRecorder(service))

    await act(() => result.current.startTimeRecord())

    startTimeRecord.should.have.callCount(1)
    result.current.state.should.deep.equal({
      records: [記録], timeRecordId: 記録ID, startedAt: 開始時刻, endedAt: null,
    })
  })

  it('開始後に古い一覧が届いても過去の記録と開始した記録が残り終了できます', async () => {
    const 過去の記録 = presentedRecord(次の記録ID, 開始時刻, 終了時刻)
    const { result, initialListResponse, endTimeRecord } = renderRecorder()

    await act(() => result.current.startTimeRecord())
    await act(async () => {
      initialListResponse.resolve([過去の記録])
      await initialListResponse.promise
    })

    result.current.state.should.deep.equal({
      records: [過去の記録, 記録],
      timeRecordId: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
    })

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    result.current.state.should.deep.equal({
      records: [過去の記録, 終了済み記録],
      timeRecordId: null,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    })
  })

  it('操作後の再読み込みはサーバーの更新と削除を反映します', async () => {
    const 過去の記録 = presentedRecord(次の記録ID, 開始時刻, 終了時刻)
    const { result, rerender, service } = renderRecorder([過去の記録])
    await waitFor(() => result.current.state.records.should.deep.equal([過去の記録]))
    await act(() => result.current.startTimeRecord())

    rerender({
      service: { ...service, getTimeRecords: async () => [終了済み記録] },
    })

    await waitFor(() => result.current.state.records.should.deep.equal([終了済み記録]))
    result.current.state.should.have.property('timeRecordId', null)
  })

  it('終了操作で現在の記録を指定して確定した終了時刻と一覧を更新します', async () => {
    const endTimeRecord = vi.fn(async () => 終了済み記録)
    const service = createTimeRecordServiceStub({
      startTimeRecord: async () => 記録, endTimeRecord,
    })
    const { result } = renderHook(() => useTimeRecorder(service))
    await act(() => result.current.startTimeRecord())

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    result.current.state.should.deep.equal({
      records: [終了済み記録], timeRecordId: null, startedAt: 開始時刻, endedAt: 終了時刻,
    })
  })

  it('再び開始すると前回の記録を残して終了時刻を消去します', async () => {
    const 次の記録 = presentedRecord(次の記録ID, 次の開始時刻)
    const startTimeRecord = vi.fn()
      .mockResolvedValueOnce(記録).mockResolvedValueOnce(次の記録)
    const service = createTimeRecordServiceStub({ startTimeRecord, endTimeRecord: async () => 終了済み記録 })
    const { result } = renderHook(() => useTimeRecorder(service))
    await act(() => result.current.startTimeRecord())
    await act(() => result.current.endTimeRecord())

    await act(() => result.current.startTimeRecord())

    result.current.state.should.deep.equal({
      records: [終了済み記録, 次の記録], timeRecordId: 次の記録ID, startedAt: 次の開始時刻, endedAt: null,
    })
  })

  it('記録中でなければ終了を要求しません', async () => {
    const endTimeRecord = vi.fn(async () => 終了済み記録)
    const service = createTimeRecordServiceStub({ endTimeRecord })
    const { result } = renderHook(() => useTimeRecorder(service))

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.callCount(0)
  })

})

// 初期一覧を省略した場合は、initialListResponseから応答を完了させます。
function renderRecorder(initialRecords?: (typeof 記録)[]) {
  const initialListResponse = deferred<(typeof 記録)[]>()
  if (initialRecords !== undefined) initialListResponse.resolve(initialRecords)
  const endTimeRecord = vi.fn(async () => 終了済み記録)
  const service = createTimeRecordServiceStub({
    getTimeRecords: () => initialListResponse.promise,
    startTimeRecord: async () => 記録,
    endTimeRecord,
  })

  return {
    ...renderHook(({ service }) => useTimeRecorder(service), { initialProps: { service } }),
    service,
    initialListResponse,
    endTimeRecord,
  }
}
