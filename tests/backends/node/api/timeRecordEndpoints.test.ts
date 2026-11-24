import { GoogleIdTokenVerifier } from '../../../../src/backends/node/infrastructure/googleIdTokenVerifier.js'
import { afterEach, beforeEach, describe, test, vi, type Mock } from 'vitest'
import Fastify from 'fastify'
import { createApp } from '../../../../src/backends/node/app.js'
import { registerTimeRecordEndpoints } from '../../../../src/backends/node/api/timeRecordEndpoints.js'
import { TimeRecords } from '../../../../src/backends/node/application/timeRecords.js'
import { timeRecordSources } from '../../../../src/backends/node/infrastructure/timeRecordSources.js'
import { InMemoryTimeRecordState } from '../../../../src/backends/node/infrastructure/timeRecordState.js'
import { createGoogleIdTokenFixture } from '../test/googleIdTokenFixture.js'

interface TimeRecordResponse {
  id: string
  startedAt: string
  endedAt: string | null
}

const 開始時刻 = new Date('2026-08-01T09:00:00.000Z')
const 終了時刻 = new Date('2026-08-01T10:30:00.000Z')
const 同一オリジンを示さないヘッダー値 = ['same-site', 'cross-site', 'none', 'unknown', undefined]
const googleTokens = createGoogleIdTokenFixture('endpoint-test-key')
const publicKeyFetch: typeof fetch = async () => googleTokens.publicKeyResponse()
function createTestApp(fetchPublicKeys: typeof fetch = publicKeyFetch) {
  return createApp(new GoogleIdTokenVerifier('kizamu-client', fetchPublicKeys))
}
function authorization(user = 'google-user', expiresInSeconds = 3600) {
  const now = Math.floor(Date.now() / 1000)
  return 'Bearer ' + googleTokens.createToken({
    iss: 'https://accounts.google.com', aud: 'kizamu-client', sub: user, iat: now - 7200, exp: now + expiresInSeconds,
  })
}
let app: ReturnType<typeof createApp>
let fetchPublicKeys: Mock<typeof fetch>

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(開始時刻)
  fetchPublicKeys = vi.fn(publicKeyFetch)
  app = createTestApp(fetchPublicKeys)
})

afterEach(async () => {
  await app.close()
  vi.useRealTimers()
})

test.each([
  { method: 'GET', url: '/api/time-records' },
  { method: 'POST', url: '/api/time-records/start' },
] as const)('記録APIにBasic認証で$method要求を送ると401が返されます', async ({ method, url }) => {
  const response = await app.inject({
    method,
    url,
    headers: { authorization: 'Basic token', 'sec-fetch-site': 'same-origin' },
  })

  response.statusCode.should.equal(401)
})

test('有効なBearer認証の方式名が小文字でも記録一覧を取得できます', async () => {
  const response = await app.inject({
    method: 'GET',
    url: '/api/time-records',
    headers: { authorization: authorization().replace('Bearer ', 'bearer ') },
  })

  response.statusCode.should.equal(200)
})

test('公開鍵の配信元が401を返すと記録一覧の取得要求には500が返されます', async () => {
  fetchPublicKeys.mockResolvedValueOnce(new Response(null, { status: 401 }))

  const response = await app.inject({
    method: 'GET',
    url: '/api/time-records',
    headers: { authorization: 'Bearer placeholder' },
  })

  fetchPublicKeys.should.toHaveBeenCalled()
  response.statusCode.should.equal(500)
})

test('認証情報のない記録一覧の取得要求には401が返されます', async () => {
  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  response.statusCode.should.equal(401)
})

test('未認証の利用者が記録の開始を要求すると401が返り、認証済み利用者の一覧は空のままです', async () => {
  const response = await postWithoutAuthorization(app, '/api/time-records/start')

  response.statusCode.should.equal(401)
  const records = await readTimeRecords(app)
  records.should.deep.equal([])
})

describe('記録を開始して終了時刻まで進めたとき', () => {
  let startedRecord: TimeRecordResponse

  beforeEach(async () => {
    startedRecord = await prepareStartedRecord(app)
    vi.setSystemTime(終了時刻)
  })

  test('未認証の利用者が記録の終了を要求すると401が返り、所有者の一覧には開始時の記録が残ります', async () => {
    const response = await postWithoutAuthorization(app, `/api/time-records/${startedRecord.id}/end`)

    response.statusCode.should.equal(401)
    const records = await readTimeRecords(app)
    records.should.deep.equal([startedRecord])
  })

  test('期限切れの認証情報で記録の終了を要求すると401が返り終了時刻は未設定のままです', async () => {
    const response = await app.inject({
      method: 'POST',
      url: `/api/time-records/${startedRecord.id}/end`,
      headers: { authorization: authorization('google-user', -3600), 'sec-fetch-site': 'same-origin' },
    })

    response.statusCode.should.equal(401)
    const records = await readTimeRecords(app)
    records.should.deep.equal([startedRecord])
    records[0].should.have.property('endedAt', null)
  })

  test('記録を終了すると、元の識別子と開始時刻が保たれ、サーバーの現在時刻が終了時刻として返されます', async () => {
    const response = await postFromSameOrigin(app, `/api/time-records/${startedRecord.id}/end`)

    response.statusCode.should.equal(200)
    const endedRecord = response.json<TimeRecordResponse>()
    endedRecord.id.should.equal(startedRecord.id)
    endedRecord.startedAt.should.equal(startedRecord.startedAt)
    endedRecord.should.have.property('endedAt', 終了時刻.toISOString())
  })

  test('二件の記録を開始すると異なる識別子が発行され、一覧には開始順で両方が返されます', async () => {
    const secondRecord = await prepareStartedRecord(app)

    const records = await readTimeRecords(app)

    secondRecord.id.should.not.equal(startedRecord.id)
    records.should.deep.equal([startedRecord, secondRecord])
  })

  test('記録を終了した後に一覧を取得するとその記録の終了時刻が反映されます', async () => {
    await prepareEndedRecord(app, startedRecord.id)

    const records = await readTimeRecords(app)

    records.should.deep.equal([{ ...startedRecord, endedAt: 終了時刻.toISOString() }])
  })

  test.each(同一オリジンを示さないヘッダー値)(
    'Sec-Fetch-Siteが同一オリジンを示さない終了要求（%s）には403が返り、一覧の記録は開始時のままです',
    async (secFetchSite) => {
      const response = await postWithFetchSite(app, `/api/time-records/${startedRecord.id}/end`, secFetchSite)

      response.statusCode.should.equal(403)
      const records = await readTimeRecords(app)
      records.should.deep.equal([startedRecord])
    },
  )
})

describe('別の利用者が開始した記録があるとき', () => {
  let startedRecord: TimeRecordResponse

  beforeEach(async () => {
    startedRecord = await prepareStartedRecord(app, 'first-user')
    vi.setSystemTime(終了時刻)
  })

  test('他の利用者が記録の終了を要求すると404が返り、所有者の一覧には元の記録が残ります', async () => {
    const response = await postFromSameOrigin(app, `/api/time-records/${startedRecord.id}/end`, 'second-user')

    response.statusCode.should.equal(404)
    const records = await readTimeRecords(app, 'first-user')
    records.should.deep.equal([startedRecord])
  })
})

test('所有者が保存先にない記録の終了を要求すると404が返ります', async () => {
  const state = new InMemoryTimeRecordState()
  const endpointApp = Fastify()
  registerTimeRecordEndpoints(
    endpointApp, new TimeRecords(state, timeRecordSources), new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch),
  )

  try {
    const startedRecord = await prepareStartedRecord(endpointApp)
    vi.setSystemTime(終了時刻)
    vi.spyOn(state, 'find').mockReturnValue(undefined)

    const response = await postFromSameOrigin(endpointApp, `/api/time-records/${startedRecord.id}/end`)

    response.statusCode.should.equal(404)
  } finally {
    await endpointApp.close()
  }
})

test('二人がそれぞれ記録を開始すると、各利用者の一覧には本人の記録だけが含まれます', async () => {
  const firstRecord = await prepareStartedRecord(app, 'first-user')
  const secondRecord = await prepareStartedRecord(app, 'second-user')
  const firstRecords = await readTimeRecords(app, 'first-user')
  const secondRecords = await readTimeRecords(app, 'second-user')
  firstRecords.should.deep.equal([firstRecord])
  secondRecords.should.deep.equal([secondRecord])
})

test('記録を開始すると、空でないGUIDとサーバーの現在時刻を持ち、終了時刻はnullの記録がHTTP 201で返されます', async () => {
  const response = await postFromSameOrigin(app, '/api/time-records/start')

  response.statusCode.should.equal(201)
  const startedRecord = response.json<TimeRecordResponse>()
  startedRecord.id.should.match(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  startedRecord.id.should.not.equal('00000000-0000-0000-0000-000000000000')
  startedRecord.startedAt.should.equal(開始時刻.toISOString())
  startedRecord.should.have.property('endedAt', null)
})

test('まだ記録を開始していない利用者が一覧を取得すると空の配列が返されます', async () => {
  const records = await readTimeRecords(app)

  records.should.deep.equal([])
})

test('同じ利用者でも、別に起動したアプリの一覧には元のアプリで開始した記録は含まれません', async () => {
  await prepareStartedRecord(app)
  const independentApp = createTestApp()
  try {
    const records = await readTimeRecords(independentApp)

    records.should.deep.equal([])
  } finally {
    await independentApp.close()
  }
})

test.each(同一オリジンを示さないヘッダー値)(
  'Sec-Fetch-Siteが同一オリジンを示さない開始要求（%s）には403が返り、一覧は空のままです',
  async (secFetchSite) => {
    const response = await postWithFetchSite(app, '/api/time-records/start', secFetchSite)

    response.statusCode.should.equal(403)
    const records = await readTimeRecords(app)
    records.should.deep.equal([])
  },
)

async function readTimeRecords(app: ReturnType<typeof createApp>, user = 'google-user'): Promise<TimeRecordResponse[]> {
  const response = await app.inject({ method: 'GET', url: '/api/time-records', headers: { authorization: authorization(user) } })
  response.statusCode.should.equal(200)
  return response.json<TimeRecordResponse[]>()
}

function postWithoutAuthorization(app: ReturnType<typeof createApp>, url: string) {
  return app.inject({ method: 'POST', url, headers: { 'sec-fetch-site': 'same-origin' } })
}

function postFromSameOrigin(app: ReturnType<typeof createApp>, url: string, user = 'google-user') {
  return postWithFetchSite(app, url, 'same-origin', user)
}

function postWithFetchSite(app: ReturnType<typeof createApp>, url: string, secFetchSite: string | undefined, user = 'google-user') {
  const headers = { authorization: authorization(user) }
  return app.inject({
    method: 'POST', url,
    headers: secFetchSite === undefined ? headers : { ...headers, 'sec-fetch-site': secFetchSite },
  })
}

async function prepareStartedRecord(app: ReturnType<typeof createApp>, user = 'google-user'): Promise<TimeRecordResponse> {
  const response = await postFromSameOrigin(app, '/api/time-records/start', user)
  response.statusCode.should.equal(201)
  return response.json<TimeRecordResponse>()
}

async function prepareEndedRecord(app: ReturnType<typeof createApp>, recordId: string): Promise<void> {
  const response = await postFromSameOrigin(app, `/api/time-records/${recordId}/end`)
  response.statusCode.should.equal(200)
}
