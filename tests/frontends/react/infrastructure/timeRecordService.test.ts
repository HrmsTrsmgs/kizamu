import { afterEach, beforeEach, describe, it, vi, type Mock } from 'vitest'
import { timeRecordService } from '../../../../src/frontends/react/src/infrastructure/timeRecordService'
import { presentedRecord, timestamp } from '../test/timeRecords'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'
const 記録中の応答 = { id: 記録ID, startedAt: 開始時刻, endedAt: null }
const 終了済みの応答 = { id: 記録ID, startedAt: 開始時刻, endedAt: 終了時刻 }
const tested = timeRecordService

describe('timeRecordService', () => {
  let fetchMock: Mock<typeof fetch>

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('記録一覧の取得では /api/time-records にGET要求を送ります', async () => {
    fetchMock.mockImplementation(async () => Response.json([]))

    await tested.getTimeRecords()

    fetchMock.should.have.been.calledOnceWith('/api/time-records')
  })

  it('一覧の応答から記録中と終了済みの記録を復元し、それぞれの識別子と時刻を返します', async () => {
    fetchMock.mockImplementation(async () => Response.json([
      記録中の応答,
      { ...終了済みの応答, id: 次の記録ID },
    ]))

    const records = await tested.getTimeRecords()
    records.should.toStrictEqual([
      presentedRecord(記録ID, timestamp(開始時刻)),
      presentedRecord(次の記録ID, timestamp(開始時刻), timestamp(終了時刻)),
    ])
  })

  it('一覧取得が成功しても応答がオブジェクトなら、呼び出し元にエラーを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json(記録中の応答))

    await tested.getTimeRecords().should.rejects.toThrow()
  })

  describe('記録開始APIが記録中の応答を返すとき', () => {
    beforeEach(() => {
      fetchMock.mockImplementation(async () => Response.json(記録中の応答))
    })

    it('記録の開始では /api/time-records/start にPOST要求を送ります', async () => {
      await tested.startTimeRecord()

      fetchMock.should.have.been.calledOnceWith('/api/time-records/start', { method: 'POST' })
    })

    it('開始応答の識別子と開始時刻から記録中の記録を復元します', async () => {
      const startedRecord = await tested.startTimeRecord()
      startedRecord.should.toStrictEqual(presentedRecord(記録ID, timestamp(開始時刻)))
    })
  })

  it.each([
    ['欠落している', {}],
    ['nullである', { id: null }],
    ['数値である', { id: 1 }],
  ])('開始要求が成功しても応答の記録IDが%s場合は、呼び出し元にエラーを返します', async (_, 応答のID) => {
    fetchMock.mockImplementation(async () => Response.json(
      { startedAt: 開始時刻, endedAt: null, ...応答のID },
      { status: 201 },
    ))

    await tested.startTimeRecord().should.rejects.toThrow()
  })

  it('開始要求が成功しても応答の開始時刻がISO文字列を1件含む配列なら、呼び出し元にエラーを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json({ ...記録中の応答, startedAt: [開始時刻] }))

    await tested.startTimeRecord().should.rejects.toThrow()
  })

  describe('記録終了APIが終了済みの応答を返すとき', () => {
    beforeEach(() => {
      fetchMock.mockImplementation(async () => Response.json(終了済みの応答))
    })

    it('記録の終了では、指定した記録IDの終了URLにPOST要求を送ります', async () => {
      await tested.endTimeRecord(記録ID)

      fetchMock.should.have.been.calledOnceWith(`/api/time-records/${記録ID}/end`, { method: 'POST' })
    })

    it('終了応答の識別子と開始・終了時刻から終了済みの記録を復元します', async () => {
      const endedRecord = await tested.endTimeRecord(記録ID)
      endedRecord.should.toStrictEqual(presentedRecord(記録ID, timestamp(開始時刻), timestamp(終了時刻)))
    })
  })

  it('終了要求が成功しても応答の終了時刻がISO文字列を1件含む配列なら、呼び出し元にエラーを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json({ ...終了済みの応答, endedAt: [終了時刻] }))

    await tested.endTimeRecord(記録ID).should.rejects.toThrow()
  })

  it('一覧取得の応答がHTTPエラーなら、本文に記録の一覧があっても呼び出し元にエラーを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json([記録中の応答], { status: 500 }))

    await tested.getTimeRecords().should.rejects.toThrow()
  })

  it('開始応答がHTTPエラーなら、本文に記録中の記録があっても呼び出し元にエラーを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json(記録中の応答, { status: 500 }))

    await tested.startTimeRecord().should.rejects.toThrow()
  })

  it('終了応答がHTTPエラーなら、本文に終了済みの記録があっても呼び出し元にエラーを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json(終了済みの応答, { status: 500 }))

    await tested.endTimeRecord(記録ID).should.rejects.toThrow()
  })

  describe('時差と小数秒を含む記録開始応答', () => {
    const 開始時刻の別表記 = '2026-08-01T18:00:00.0000000+09:00'

    beforeEach(() => {
      fetchMock.mockImplementation(async () => Response.json({ ...記録中の応答, startedAt: 開始時刻の別表記 }))
    })

    it('開始応答に含まれる時差と小数秒を元のISO表記のまま保持します', async () => {
      const record = await tested.startTimeRecord()

      record.isoText.startedAt.should.equal(開始時刻の別表記)
    })

    it('時差付きの開始時刻をUTC表記と同じ日時点として復元します', async () => {
      const record = await tested.startTimeRecord()

      record.record.startedAt.should.deep.equal(timestamp(開始時刻).instant)
    })
  })
})
