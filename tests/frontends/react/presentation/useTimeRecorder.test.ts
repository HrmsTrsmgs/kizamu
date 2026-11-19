import { act, renderHook, waitFor } from '@testing-library/react'
import { Project } from 'kizamu-domain-typescript'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Mock } from 'vitest'
import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'
import { useTimeRecorder } from '../../../../src/frontends/react/src/presentation/useTimeRecorder'
import { deferred } from '../test/deferred'
import { presentedRecord, timestamp } from '../test/timeRecords'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 開始時刻 = timestamp('2026-08-01T09:00:00Z')
const 終了時刻 = timestamp('2026-08-01T10:30:00Z')
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
    service = { getTimeRecords, startTimeRecord, endTimeRecord }
  })

  describe('開始要求と同時に開始と終了を追加で要求したとき', () => {
    let recorder: ReturnType<typeof 開始応答を保留する>
    let operation: Promise<void>

    beforeEach(() => {
      recorder = 開始応答を保留する()
      act(() => {
        operation = recorder.result.current.startTimeRecord()
        void recorder.result.current.startTimeRecord()
        void recorder.result.current.endTimeRecord()
      })
    })

    it('記録の開始応答を待つ間は、追加の開始要求も終了要求も送りません', async () => {
      startTimeRecord.should.have.callCount(1)
      endTimeRecord.should.have.callCount(0)

      await act(async () => {
        recorder.response.resolve(記録)
        await operation
      })
    })

    it('開始要求が成功した後は、その記録の終了を要求できます', async () => {
      await act(async () => {
        recorder.response.resolve(記録)
        await operation
      })

      await act(() => recorder.result.current.endTimeRecord())

      endTimeRecord.should.have.callCount(1)
    })

    it('記録の開始に失敗し、再取得した一覧が空なら開始を再度要求できます', async () => {
      await act(async () => {
        recorder.response.reject(new Error('failed'))
        await operation.should.rejects.toThrow('failed')
      })

      await act(() => recorder.result.current.startTimeRecord())

      startTimeRecord.should.have.callCount(2)
    })
  })

  describe('終了要求と同時に開始と終了を追加で要求したとき', () => {
    let recorder: ReturnType<typeof 終了応答を保留する>
    let operation: Promise<void>

    beforeEach(async () => {
      recorder = 終了応答を保留する()
      await act(() => recorder.result.current.startTimeRecord())
      act(() => {
        operation = recorder.result.current.endTimeRecord()
        void recorder.result.current.startTimeRecord()
        void recorder.result.current.endTimeRecord()
      })
    })

    it('記録の終了応答を待つ間は、追加の開始要求も終了要求も送りません', async () => {
      startTimeRecord.should.have.callCount(1)
      endTimeRecord.should.have.callCount(1)

      await act(async () => {
        recorder.response.resolve(終了済み記録)
        await operation
      })
    })

    it('終了要求が成功した後は、新しい記録の開始を要求できます', async () => {
      await act(async () => {
        recorder.response.resolve(終了済み記録)
        await operation
      })

      await act(() => recorder.result.current.startTimeRecord())

      startTimeRecord.should.have.callCount(2)
    })

    it('記録の終了に失敗し、再取得した一覧で記録中なら終了を再度要求できます', async () => {
      await act(async () => {
        recorder.response.reject(new Error('failed'))
        await operation.should.rejects.toThrow('failed')
      })

      await act(() => recorder.result.current.endTimeRecord())

      endTimeRecord.should.have.callCount(2)
    })
  })

  describe('開始の通信に失敗して一覧を再取得できるとき', () => {
    const 開始失敗 = new Error('failed')
    let recorder: ReturnType<typeof renderRecorder>

    beforeEach(async () => {
      getTimeRecords.mockResolvedValueOnce([]).mockResolvedValueOnce([記録])
      startTimeRecord.mockRejectedValue(開始失敗)
      recorder = renderRecorder()
      await waitFor(() => getTimeRecords.should.have.callCount(1))
    })

    it('記録の開始に失敗して一覧を再取得できた場合、呼び出し元には開始時のエラーが届きます', async () => {
      await act(async () => {
        await expect(recorder.result.current.startTimeRecord()).rejects.toBe(開始失敗)
      })
    })

    it('開始に失敗しても、再取得した一覧に記録中の作業があれば、その一覧を保持し、その作業を終了対象にします', async () => {
      await act(async () => {
        await expect(recorder.result.current.startTimeRecord()).rejects.toThrow()
      })

      getTimeRecords.should.have.callCount(2)
      recorder.result.current.state.records.should.deep.equal([記録])
      recorder.result.current.state.should.have.property('timeRecordId', 記録ID)
    })
  })

  describe('終了の通信に失敗して一覧を再取得できるとき', () => {
    const 終了失敗 = new Error('failed')
    let recorder: ReturnType<typeof renderRecorder>

    beforeEach(async () => {
      getTimeRecords.mockResolvedValueOnce([記録]).mockResolvedValueOnce([終了済み記録])
      endTimeRecord.mockRejectedValue(終了失敗)
      recorder = renderRecorder()
      await waitFor(() => recorder.result.current.state.should.have.property('timeRecordId', 記録ID))
    })

    it('記録の終了に失敗して一覧を再取得できた場合、呼び出し元には終了時のエラーが届きます', async () => {
      await act(async () => {
        await expect(recorder.result.current.endTimeRecord()).rejects.toBe(終了失敗)
      })
    })

    it('終了に失敗しても、再取得した一覧で作業が終了済みなら、その一覧を保持し、終了対象を解除します', async () => {
      await act(async () => {
        await expect(recorder.result.current.endTimeRecord()).rejects.toThrow()
      })

      getTimeRecords.should.have.callCount(2)
      recorder.result.current.state.records.should.deep.equal([終了済み記録])
      recorder.result.current.state.should.have.property('timeRecordId', null)
    })
  })

  describe('終了済みの一覧を読み込み、開始要求と再取得に失敗するとき', () => {
    let result: ReturnType<typeof renderRecorder>['result']
    let 再取得失敗: Error

    beforeEach(async () => {
      getTimeRecords.mockResolvedValueOnce([終了済み記録])
      result = renderRecorder().result
      await waitFor(() => result.current.state.records.should.toStrictEqual([終了済み記録]))

      startTimeRecord.mockRejectedValueOnce(new Error('開始失敗'))
      再取得失敗 = new Error('一覧取得失敗')
      getTimeRecords.mockRejectedValue(再取得失敗)
    })

    it('開始失敗後の一覧再取得にも失敗したら呼び出し元に一覧取得のエラーを返します', async () => {
      await act(async () => {
        await expect(result.current.startTimeRecord()).rejects.toBe(再取得失敗)
      })
    })

    describe('開始要求と再取得が失敗した後', () => {
      beforeEach(async () => {
        await act(async () => {
          await expect(result.current.startTimeRecord()).rejects.toThrow()
        })
      })

      it('開始失敗後の一覧再取得にも失敗すると直前の一覧を残して未確認状態になります', () => {
        result.current.state.records.should.toStrictEqual([終了済み記録])
        result.current.should.have.property('isStateUnconfirmed', true)
        result.current.should.have.property('isBusy', false)
      })

      it('開始失敗後の状態を確認できるまでは追加の開始と終了を要求しません', async () => {
        await act(() => result.current.startTimeRecord())
        await act(() => result.current.endTimeRecord())

        startTimeRecord.should.have.callCount(1)
        endTimeRecord.should.have.callCount(0)
        getTimeRecords.should.have.callCount(2)
      })

      describe('手動再取得で記録中の一覧が返るとき', () => {
        beforeEach(() => {
          getTimeRecords.mockResolvedValue([記録])
        })

        it('手動再取得した記録中の一覧を確認済みにして終了対象を復元します', async () => {
          await act(() => result.current.reloadTimeRecords())

          result.current.state.records.should.toStrictEqual([記録])
          result.current.should.have.property('isStateUnconfirmed', false)
          result.current.state.should.have.property('timeRecordId', 記録ID)
        })

        it('手動再取得で記録中の一覧を確認した後はその記録を終了できます', async () => {
          await act(() => result.current.reloadTimeRecords())

          await act(() => result.current.endTimeRecord())

          endTimeRecord.should.have.been.calledOnceWith(記録ID)
        })
      })

      it('手動再取得にも失敗すると直前の一覧と未確認状態を保ちます', async () => {
        await act(async () => {
          await expect(result.current.reloadTimeRecords()).rejects.toThrow('一覧取得失敗')
        })

        result.current.state.records.should.toStrictEqual([終了済み記録])
        result.current.should.have.property('isStateUnconfirmed', true)
        result.current.should.have.property('isBusy', false)
        getTimeRecords.should.have.callCount(3)
      })

      it('手動再取得の通信中は再取得と開始と終了の追加要求を送りません', async () => {
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
    })
  })

  describe('初回一覧の応答を待つ間に次の一覧取得が失敗するとき', () => {
    let 初期一覧の応答: ReturnType<typeof deferred<(typeof 記録)[]>>

    beforeEach(() => {
      初期一覧の応答 = deferred<(typeof 記録)[]>()
      getTimeRecords.mockReturnValueOnce(初期一覧の応答.promise).mockRejectedValueOnce(new Error('一覧取得失敗'))
    })

    it('開始と再取得に失敗した後に古い初回一覧が届いても、状態は未確認のまま、開始・終了の追加要求は送りません', async () => {
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

    it('手動の一覧取得に失敗してから記録を開始すると、後から届いた初回一覧にその記録が加わり、終了対象になります', async () => {
      const { result } = renderRecorder()

      await act(async () => {
        await result.current.reloadTimeRecords().should.rejects.toThrow('一覧取得失敗')
      })
      await act(() => result.current.startTimeRecord())
      await act(async () => {
        初期一覧の応答.resolve([])
        await 初期一覧の応答.promise
      })

      result.current.state.records.should.toStrictEqual([記録])
      result.current.state.should.have.property('timeRecordId', 記録ID)
    })
  })

  describe('記録中の一覧を読み込み、終了要求と再取得に失敗するとき', () => {
    let result: ReturnType<typeof renderRecorder>['result']
    let 再取得失敗: Error

    beforeEach(async () => {
      getTimeRecords.mockResolvedValueOnce([記録])
      result = renderRecorder().result
      await waitFor(() => result.current.state.should.have.property('timeRecordId', 記録ID))

      endTimeRecord.mockRejectedValueOnce(new Error('終了失敗'))
      再取得失敗 = new Error('一覧取得失敗')
      getTimeRecords.mockRejectedValue(再取得失敗)
    })

    it('終了失敗後の一覧再取得にも失敗したら呼び出し元に一覧取得のエラーを返します', async () => {
      await act(async () => {
        await expect(result.current.endTimeRecord()).rejects.toBe(再取得失敗)
      })
    })

    describe('終了要求と再取得が失敗した後', () => {
      beforeEach(async () => {
        await act(async () => {
          await expect(result.current.endTimeRecord()).rejects.toThrow()
        })
      })

      it('終了失敗後の一覧再取得にも失敗すると直前の一覧を残して未確認状態になります', () => {
        result.current.state.records.should.toStrictEqual([記録])
        result.current.should.have.property('isStateUnconfirmed', true)
        result.current.should.have.property('isBusy', false)
      })

      it('終了失敗後の状態を確認できるまでは追加の終了と開始を要求しません', async () => {
        await act(() => result.current.endTimeRecord())
        await act(() => result.current.startTimeRecord())

        endTimeRecord.should.have.been.calledOnceWith(記録ID)
        startTimeRecord.should.have.callCount(0)
        getTimeRecords.should.have.callCount(2)
      })

      describe('手動再取得で終了済みの一覧が返るとき', () => {
        beforeEach(() => {
          getTimeRecords.mockResolvedValue([終了済み記録])
        })

        it('手動再取得した終了済みの一覧を確認済みにして終了対象を解除します', async () => {
          await act(() => result.current.reloadTimeRecords())

          result.current.state.records.should.toStrictEqual([終了済み記録])
          result.current.should.have.property('isStateUnconfirmed', false)
          result.current.state.should.have.property('timeRecordId', null)
        })

        it('手動再取得で終了済みの一覧を確認した後は新しい記録を開始できます', async () => {
          await act(() => result.current.reloadTimeRecords())

          await act(() => result.current.startTimeRecord())

          startTimeRecord.should.have.callCount(1)
        })
      })
    })
  })

  describe('初期一覧に終了済みと記録中の記録があるとき', () => {
    const 一覧の記録中の記録 = presentedRecord(次の記録ID, 開始時刻)

    beforeEach(() => {
      getTimeRecords.mockResolvedValue([終了済み記録, 一覧の記録中の記録])
    })

    it('初期化時に作業記録の一覧を一度だけ要求します', async () => {
      const { result } = renderRecorder()

      await waitFor(() => result.current.state.records.should.have.lengthOf(2))
      getTimeRecords.should.have.callCount(1)
    })

    it('初回一覧に終了済みと記録中の作業があると、両方を一覧に載せ、記録中の作業を終了対象にします', async () => {
      const { result } = renderRecorder()

      await waitFor(() => result.current.state.records.should.toStrictEqual([終了済み記録, 一覧の記録中の記録]))
      result.current.state.should.have.property('timeRecordId', 次の記録ID)
    })
  })

  it('開始要求に成功すると、応答の記録を一覧に追加し、操作欄の表示対象と終了対象にします', async () => {
    const { result } = renderRecorder()

    await act(() => result.current.startTimeRecord())

    startTimeRecord.should.have.callCount(1)
    result.current.state.should.toStrictEqual({
      records: [記録], timeRecordId: 記録ID, displayedRecord: 記録,
    })
  })

  it('記録の開始応答を待つ間にプロジェクトを切り替えても、応答の記録は開始元のプロジェクトの終了対象になります', async () => {
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

  describe('記録開始の後に古い初期一覧が届くとき', () => {
    const 過去の記録 = presentedRecord(次の記録ID, 開始時刻, 終了時刻)
    let recorder: ReturnType<typeof renderRecorder>

    beforeEach(async () => {
      const initialListResponse = deferred<(typeof 記録)[]>()
      getTimeRecords.mockReturnValue(initialListResponse.promise)
      recorder = renderRecorder()
      await act(() => recorder.result.current.startTimeRecord())

      await act(async () => {
        initialListResponse.resolve([過去の記録])
        await initialListResponse.promise
      })
    })

    it('記録開始後に古い初回一覧が届くと、過去の記録と新しい記録が一覧に残り、新しい記録が終了対象になります', () => {
      recorder.result.current.state.should.toStrictEqual({
        records: [過去の記録, 記録],
        timeRecordId: 記録ID,
        displayedRecord: 記録,
      })
    })

    it('古い一覧を受け取った後でも開始した記録を指定して終了できます', async () => {
      await act(() => recorder.result.current.endTimeRecord())

      endTimeRecord.should.have.been.calledOnceWith(記録ID)
      recorder.result.current.state.should.toStrictEqual({
        records: [過去の記録, 終了済み記録],
        timeRecordId: null,
        displayedRecord: 終了済み記録,
      })
    })
  })

  it('記録開始後に一覧を読み直すと、サーバーで終了した記録を更新し、サーバーから消えた記録を一覧から除きます', async () => {
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

  it('終了を要求すると記録中のIDが送られ、応答の終了済み記録で一覧と操作欄を更新します', async () => {
    const { result } = await renderStartedRecorder()

    await act(() => result.current.endTimeRecord())

    endTimeRecord.should.have.been.calledOnceWith(記録ID)
    result.current.state.should.toStrictEqual({
      records: [終了済み記録], timeRecordId: null, displayedRecord: 終了済み記録,
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

  function 開始応答を保留する() {
    const response = deferred<typeof 記録>()
    startTimeRecord.mockReturnValueOnce(response.promise)
    const { result } = renderRecorder()
    return { response, result }
  }

  function 終了応答を保留する() {
    const response = deferred<typeof 終了済み記録>()
    endTimeRecord.mockReturnValueOnce(response.promise)
    getTimeRecords.mockResolvedValueOnce([]).mockResolvedValue([記録])
    const { result } = renderRecorder()
    return { response, result }
  }
})
