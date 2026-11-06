import { afterEach, describe, it, vi } from 'vitest'
import { timeRecordService } from '../../../../src/frontends/react/src/infrastructure/timeRecordService'
import { presentedRecord, timestamp } from '../test/timeRecords'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 次の記録ID = '019cdeac-b600-7000-8000-000000000002'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'
const tested = timeRecordService

describe('timeRecordService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('作業記録一覧APIへGETします', async () => {
    const fetchMock = vi.fn(async () => response([]))
    vi.stubGlobal('fetch', fetchMock)

    await tested.getTimeRecords()

    fetchMock.should.have.callCount(1)
    fetchMock.should.have.been.calledWith('/api/time-records')
  })

  it('作業記録一覧APIのDTOを内部の日時点に変換します', async () => {
    const expected = [
      { id: 記録ID, startedAt: 開始時刻, endedAt: null },
      { id: 次の記録ID, startedAt: 開始時刻, endedAt: 終了時刻 },
    ]
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    const records = await tested.getTimeRecords()
    records.should.toStrictEqual([
      presentedRecord(記録ID, timestamp(開始時刻)),
      presentedRecord(次の記録ID, timestamp(開始時刻), timestamp(終了時刻)),
    ])
  })

  it('記録開始APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
    }))
    vi.stubGlobal('fetch', fetchMock)

    await tested.startTimeRecord()

    fetchMock.should.have.callCount(1)
    fetchMock.should.have.been.calledWith('/api/time-records/start', { method: 'POST' })
  })

  it('記録開始APIのDTOを内部の日時点に変換します', async () => {
    const expected = {
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: null,
    }
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    const startedRecord = await tested.startTimeRecord()
    startedRecord.should.toStrictEqual(presentedRecord(記録ID, timestamp(開始時刻)))
  })

  it('指定した記録の終了APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }))
    vi.stubGlobal('fetch', fetchMock)

    await tested.endTimeRecord(記録ID)

    fetchMock.should.have.callCount(1)
    fetchMock.should.have.been.calledWith(`/api/time-records/${記録ID}/end`, { method: 'POST' })
  })

  it('記録終了APIのDTOを内部の日時点に変換します', async () => {
    const expected = {
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    const endedRecord = await tested.endTimeRecord(expected.id)
    endedRecord.should.toStrictEqual(presentedRecord(記録ID, timestamp(開始時刻), timestamp(終了時刻)))
  })

  it('一覧のDTOから復元した記録は元を変更せず終了操作を利用できます', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response([
      { id: 記録ID, startedAt: 開始時刻, endedAt: null },
    ])))
    const [{ record }] = await tested.getTimeRecords()

    const endedRecord = record.end(timestamp(終了時刻).instant)

    endedRecord.id.should.equal(記録ID)
    endedRecord.startedAt.should.deep.equal(timestamp(開始時刻).instant)
    endedRecord.should.have.property('endedAt').that.deep.equal(timestamp(終了時刻).instant)
    endedRecord.should.not.equal(record)
    record.should.have.property('endedAt', null)
  })

  it('応答の時差と小数秒の表記を保持して日時点に変換します', async () => {
    const 開始時刻の別表記 = '2026-08-01T18:00:00.0000000+09:00'
    vi.stubGlobal('fetch', vi.fn(async () => response({
      id: 記録ID, startedAt: 開始時刻の別表記, endedAt: null,
    })))

    const record = await tested.startTimeRecord()

    record.isoText.startedAt.should.equal(開始時刻の別表記)
    record.record.startedAt.should.deep.equal(timestamp(開始時刻).instant)
  })
})

function response(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
  })
}
