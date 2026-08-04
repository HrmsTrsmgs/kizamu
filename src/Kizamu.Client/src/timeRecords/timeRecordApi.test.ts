import { afterEach, describe, expect, it, vi } from 'vitest'
import { timeRecordApi } from './timeRecordApi'

describe('timeRecordApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('記録開始APIへPOSTします', async () => {
    const fetchMock = vi.fn(async () => response({
      id: '019cde5b-4c00-7000-8000-000000000001',
      startedAt: '2026-08-01T09:00:00Z',
    }))
    vi.stubGlobal('fetch', fetchMock)

    await timeRecordApi.startTimeRecord()

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith('/api/time-records/start', { method: 'POST' })
  })

  it('記録開始APIのレスポンスを返します', async () => {
    const expected = {
      id: '019cde5b-4c00-7000-8000-000000000001',
      startedAt: '2026-08-01T09:00:00Z',
    }
    vi.stubGlobal('fetch', vi.fn(async () => response(expected)))

    const result = await timeRecordApi.startTimeRecord()

    expect(result).toEqual(expected)
  })

  it('指定した記録の終了APIへPOSTします', async () => {
    const id = '019cde5b-4c00-7000-8000-000000000001'
    const fetchMock = vi.fn(async () => response({
      id,
      startedAt: '2026-08-01T09:00:00Z',
      endedAt: '2026-08-01T10:30:00Z',
    }))
    vi.stubGlobal('fetch', fetchMock)

    await timeRecordApi.endTimeRecord(id)

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith(`/api/time-records/${id}/end`, { method: 'POST' })
  })

  it('記録終了APIのレスポンスを返します', async () => {
    const expected = {
      id: '019cde5b-4c00-7000-8000-000000000001',
      startedAt: '2026-08-01T09:00:00Z',
      endedAt: '2026-08-01T10:30:00Z',
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
