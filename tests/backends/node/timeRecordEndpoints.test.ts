import { afterEach, beforeEach, test, vi } from 'vitest'
import { createApp } from '../../../src/backends/node/app.js'

interface TimeRecordResponse {
  id: string
  startedAt: string
  endedAt: string | null
}

const 開始時刻 = new Date('2026-08-01T09:00:00.000Z')
const 終了時刻 = new Date('2026-08-01T10:30:00.000Z')
let app: ReturnType<typeof createApp>

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(開始時刻)
  app = createApp()
})

afterEach(async () => {
  await app.close()
  vi.useRealTimers()
})

test('記録を開始すると空でないGUIDとサーバーの開始時刻があり終了時刻がない記録が返されます', async () => {
  const response = await postFromSameOrigin(app, '/api/time-records/start')

  response.statusCode.should.equal(201)
  const startedRecord = response.json<TimeRecordResponse>()
  startedRecord.id.should.match(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  startedRecord.id.should.not.equal('00000000-0000-0000-0000-000000000000')
  new Date(startedRecord.startedAt).should.deep.equal(開始時刻)
  startedRecord.startedAt.should.equal(開始時刻.toISOString())
  startedRecord.should.have.property('endedAt', null)
})

test('記録を終了すると同じ識別子と元の開始時刻とサーバーの終了時刻が返されます', async () => {
  const startedRecord = await prepareStartedRecord(app)
  vi.setSystemTime(終了時刻)

  const response = await postFromSameOrigin(app, `/api/time-records/${startedRecord.id}/end`)

  response.statusCode.should.equal(200)
  const endedRecord = response.json<TimeRecordResponse>()
  endedRecord.id.should.equal(startedRecord.id)
  endedRecord.startedAt.should.equal(startedRecord.startedAt)
  new Date(endedRecord.endedAt ?? '').should.deep.equal(終了時刻)
  endedRecord.should.have.property('endedAt', 終了時刻.toISOString())
})

test('まだ開始していないときに一覧を取得すると空の配列が返されます', async () => {
  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  response.statusCode.should.equal(200)
  const content = { body: response.json() }
  content.should.have.property('body').that.deep.equal([])
})

test('二件の記録を開始した後に一覧を取得すると開始した順に両方が返されます', async () => {
  const firstRecord = await prepareStartedRecord(app)
  vi.setSystemTime(終了時刻)
  const secondRecord = await prepareStartedRecord(app)
  secondRecord.id.should.not.equal(firstRecord.id)

  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  response.statusCode.should.equal(200)
  const content = { body: response.json() }
  content.should.have.property('body').that.deep.equal([firstRecord, secondRecord])
})

test('記録を終了した後に一覧を取得するとその記録の終了時刻が反映されます', async () => {
  const startedRecord = await prepareStartedRecord(app)
  vi.setSystemTime(終了時刻)
  await prepareEndedRecord(app, startedRecord.id)

  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  response.statusCode.should.equal(200)
  const content = { body: response.json() }
  content.should.have.property('body').that.deep.equal([{ ...startedRecord, endedAt: 終了時刻.toISOString() }])
})

test('別のアプリで開始した記録は新しく生成したアプリの一覧には含まれません', async () => {
  const startResponse = await postFromSameOrigin(app, '/api/time-records/start')
  startResponse.statusCode.should.equal(201)
  const independentApp = createApp()
  try {
    const response = await independentApp.inject({ method: 'GET', url: '/api/time-records' })

    response.statusCode.should.equal(200)
    const content = { body: response.json() }
    content.should.have.property('body').that.deep.equal([])
  } finally {
    await independentApp.close()
  }
})

test.each(['same-site', 'cross-site', 'none', 'unknown', undefined])(
  'Sec-Fetch-Siteが同一オリジンを示さない開始要求は403になり記録が追加されません（%s）',
  async (secFetchSite) => {
    const response = await app.inject({
      method: 'POST',
      url: '/api/time-records/start',
      headers: secFetchSite === undefined ? {} : { 'sec-fetch-site': secFetchSite },
    })
    response.statusCode.should.equal(403)

    const listResponse = await app.inject({ method: 'GET', url: '/api/time-records' })
    listResponse.statusCode.should.equal(200)
    listResponse.json<TimeRecordResponse[]>().should.deep.equal([])
  },
)

test.each(['same-site', 'cross-site', 'none', 'unknown', undefined])(
  'Sec-Fetch-Siteが同一オリジンを示さない終了要求は403になり記録が終了されません（%s）',
  async (secFetchSite) => {
    const startedRecord = await prepareStartedRecord(app)
    vi.setSystemTime(終了時刻)

    const response = await app.inject({
      method: 'POST',
      url: `/api/time-records/${startedRecord.id}/end`,
      headers: secFetchSite === undefined ? {} : { 'sec-fetch-site': secFetchSite },
    })
    response.statusCode.should.equal(403)

    const listResponse = await app.inject({ method: 'GET', url: '/api/time-records' })
    listResponse.statusCode.should.equal(200)
    listResponse.json<TimeRecordResponse[]>().should.deep.equal([startedRecord])
  },
)

function postFromSameOrigin(app: ReturnType<typeof createApp>, url: string) {
  return app.inject({
    method: 'POST', url, headers: { 'sec-fetch-site': 'same-origin' },
  })
}

async function prepareStartedRecord(app: ReturnType<typeof createApp>): Promise<TimeRecordResponse> {
  const response = await postFromSameOrigin(app, '/api/time-records/start')
  response.statusCode.should.equal(201)
  return response.json<TimeRecordResponse>()
}

async function prepareEndedRecord(app: ReturnType<typeof createApp>, recordId: string): Promise<void> {
  const response = await postFromSameOrigin(app, `/api/time-records/${recordId}/end`)
  response.statusCode.should.equal(200)
}
