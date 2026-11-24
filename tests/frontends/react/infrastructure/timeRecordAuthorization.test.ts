import { afterEach, beforeEach, describe, it, vi, type Mock } from 'vitest'
import { createTimeRecordService } from '../../../../src/frontends/react/src/infrastructure/timeRecordService'
import type { TimeRecordService } from '../../../../src/frontends/react/src/presentation/timeRecordService'

const 記録ID = '019cde5b-4c00-7000-8000-000000000001'
const 開始時刻 = '2026-08-01T09:00:00Z'
const 終了時刻 = '2026-08-01T10:30:00Z'

describe('記録APIの認証', () => {
  let fetchMock: Mock<typeof fetch>

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => vi.unstubAllGlobals())

  it.each([
    ['一覧', {
      operation: (service: TimeRecordService) => service.getTimeRecords(),
      response: [],
      path: '/api/time-records',
      method: 'GET',
    }],
    ['開始', {
      operation: (service: TimeRecordService) => service.startTimeRecord(),
      response: { id: 記録ID, startedAt: 開始時刻, endedAt: null },
      path: '/api/time-records/start',
      method: 'POST',
    }],
    ['終了', {
      operation: (service: TimeRecordService) => service.endTimeRecord(記録ID),
      response: { id: 記録ID, startedAt: 開始時刻, endedAt: 終了時刻 },
      path: `/api/time-records/${記録ID}/end`,
      method: 'POST',
    }],
  ] as const)('記録の%s要求では、指定したIDトークンをAuthorizationヘッダーにBearer形式で送ります', async (_, { operation, response, path, method }) => {
    fetchMock.mockImplementation(async () => Response.json(response))
    const tested = createTimeRecordService('id-token')

    await operation(tested)

    fetchMock.mock.calls.length.should.equal(1)
    const [url, init] = fetchMock.mock.calls[0]
    url.should.equal(path)
    ;(init?.method ?? 'GET').should.equal(method)
    ;({ authorization: new Headers(init?.headers).get('Authorization') }).should.have.property('authorization', 'Bearer id-token')
  })
})
