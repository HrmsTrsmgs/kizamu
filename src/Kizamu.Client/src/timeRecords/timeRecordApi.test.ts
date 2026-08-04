import { afterEach, describe, expect, it, vi } from 'vitest'
import { timeRecordApi } from './timeRecordApi'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'

describe('timeRecordApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('記録開始APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: 記録ID,
      startedAt: 開始時刻,
    }))
    vi.stubGlobal('fetch', fetchMock)

    await timeRecordApi.startTimeRecord()

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith('/api/time-records/start', { method: 'POST' })
  })

  it('記録開始APIのレスポンスを返します', async () => {
    const expected = {
      id: 記録ID,
      startedAt: 開始時刻,
    }
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    const result = await timeRecordApi.startTimeRecord()

    expect(result).toEqual(expected)
  })

  it('指定した記録の終了APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: 記録ID,
      startedAt: 開始時刻,
      endedAt: 終了時刻,
    }))
    vi.stubGlobal('fetch', fetchMock)

    await timeRecordApi.endTimeRecord(記録ID)

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

    const result = await timeRecordApi.endTimeRecord(expected.id)

    expect(result).toEqual(expected)
  })
})

function response(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
  })
}
