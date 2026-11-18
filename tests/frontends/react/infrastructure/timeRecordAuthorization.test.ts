import { afterEach, beforeEach, describe, it, vi, type Mock } from 'vitest'
import { createTimeRecordService } from '../../../../src/frontends/react/src/infrastructure/timeRecordService'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'

describe('記録APIの認証', () => {
  let fetchMock: Mock<typeof fetch>

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => vi.unstubAllGlobals())

  it.each([
    ['一覧', '/api/time-records', 'GET'],
    ['開始', '/api/time-records/start', 'POST'],
    ['終了', `/api/time-records/${記録ID}/end`, 'POST'],
  ])('%s要求へ利用可能な認証トークンを送ります', async (operation, path, method) => {
    fetchMock.mockImplementation(async () => Response.json(operation === '一覧'
      ? []
      : { id: 記録ID, startedAt: '2026-08-01T09:00:00Z', endedAt: null }))
    const tested = createTimeRecordService('id-token')

    if (operation === '一覧') await tested.getTimeRecords()
    if (operation === '開始') await tested.startTimeRecord()
    if (operation === '終了') await tested.endTimeRecord(記録ID)

    fetchMock.mock.calls.length.should.equal(1)
    const [url, init] = fetchMock.mock.calls[0]
    url.should.equal(path)
    ;(init?.method ?? 'GET').should.equal(method)
    ;({ authorization: new Headers(init?.headers).get('Authorization') }).should.have.property('authorization', 'Bearer id-token')
  })
})
