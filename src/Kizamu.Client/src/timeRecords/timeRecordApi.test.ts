import { afterEach, describe, expect, it, vi } from 'vitest'
import { timeRecordApi } from './timeRecordApi'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'
const tested = timeRecordApi

describe('timeRecordApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('作業記録一覧APIへGETします', async () => {
    const fetchMock = vi.fn(async () => response([]))
    vi.stubGlobal('fetch', fetchMock)

    await tested.getTimeRecords()

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith('/api/time-records')
  })

  it('記録開始APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: 記録ID,
      startedAt: 開始時刻,
    }))
    vi.stubGlobal('fetch', fetchMock)

    await tested.startTimeRecord()

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith('/api/time-records/start', { method: 'POST' })
  })

  it('記録開始APIのレスポンスを返します', async () => {
    const expected = {
      id: 記録ID,
      startedAt: 開始時刻,
    }
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    expect(await tested.startTimeRecord()).toEqual(expected)
  })

  it('指定した記録の終了APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }))
    vi.stubGlobal('fetch', fetchMock)

    await tested.endTimeRecord(記録ID)

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith(`/api/time-records/${記録ID}/end`, { method: 'POST' })
  })

  it('記録終了APIのレスポンスを返します', async () => {
    const expected = {
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    expect(await tested.endTimeRecord(expected.id)).toEqual(expected)
  })
})

function response(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
  })
}
