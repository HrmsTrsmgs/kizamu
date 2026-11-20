import { generateKeyPairSync, sign } from 'node:crypto'
import { GoogleIdTokenVerifier } from '../../../../src/backends/node/infrastructure/googleIdTokenVerifier.js'
import { afterEach, beforeEach, describe, test, vi } from 'vitest'
import { createApp } from '../../../../src/backends/node/app.js'

interface TimeRecordResponse {
  id: string
  startedAt: string
  endedAt: string | null
}

const 開始時刻 = new Date('2026-08-01T09:00:00.000Z')
const 終了時刻 = new Date('2026-08-01T10:30:00.000Z')
const 同一オリジンを示さないヘッダー値 = ['same-site', 'cross-site', 'none', 'unknown', undefined]
const key = generateKeyPairSync('rsa', { modulusLength: 2048 })
const publicKeyFetch: typeof fetch = async () => Response.json({
  keys: [{ ...key.publicKey.export({ format: 'jwk' }), kid: 'endpoint-test-key', alg: 'RS256', use: 'sig' }],
})
function createTestApp() { return createApp(new GoogleIdTokenVerifier('kizamu-client', publicKeyFetch)) }
function authorization(user = 'google-user') {
  const now = Math.floor(Date.now() / 1000)
  const header = Buffer.from(JSON.stringify({ alg: 'RS256', kid: 'endpoint-test-key' })).toString('base64url')
  const payload = Buffer.from(JSON.stringify({ iss: 'https://accounts.google.com', aud: 'kizamu-client', sub: user, iat: now - 7200, exp: now + 3600 })).toString('base64url')
  const content = header + '.' + payload
  return 'Bearer ' + content + '.' + sign('RSA-SHA256', Buffer.from(content), key.privateKey).toString('base64url')
}
let app: ReturnType<typeof createApp>

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(開始時刻)
  app = createTestApp()
})

afterEach(async () => {
  await app.close()
  vi.useRealTimers()
})

test.each(['GET', 'POST'] as const)('記録APIにBasic認証で%s要求を送ると401が返されます', async (method) => {
  const response = await app.inject({
    method,
    url: method === 'GET' ? '/api/time-records' : '/api/time-records/start',
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


test('認証情報のない記録一覧の取得要求には401が返されます', async () => {
  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  response.statusCode.should.equal(401)
})

test('認証情報のない記録開始要求には401が返されます', async () => {
  const response = await postWithoutAuthorization(app, '/api/time-records/start')

  response.statusCode.should.equal(401)
})

test('認証情報のない記録開始要求を送っても一覧には記録が追加されません', async () => {
  await postWithoutAuthorization(app, '/api/time-records/start')

  const records = await readTimeRecords(app)
  records.should.deep.equal([])
})

describe('記録を開始して終了時刻まで進めたとき', () => {
  let startedRecord: TimeRecordResponse

  beforeEach(async () => {
    startedRecord = await prepareStartedRecord(app)
    vi.setSystemTime(終了時刻)
  })

  test('認証情報のない記録終了要求には401が返されます', async () => {
    const response = await postWithoutAuthorization(app, `/api/time-records/${startedRecord.id}/end`)

    response.statusCode.should.equal(401)
  })

  test('認証情報のない記録終了要求を送っても一覧の記録は開始時のままです', async () => {
    await postWithoutAuthorization(app, `/api/time-records/${startedRecord.id}/end`)

    const records = await readTimeRecords(app)
    records.should.deep.equal([startedRecord])
  })

  test('記録を終了すると、元の識別子と開始時刻が保たれ、サーバーの現在時刻が終了時刻として返されます', async () => {
    const response = await postFromSameOrigin(app, `/api/time-records/${startedRecord.id}/end`)

    response.statusCode.should.equal(200)
    const endedRecord = response.json<TimeRecordResponse>()
    endedRecord.id.should.equal(startedRecord.id)
    endedRecord.startedAt.should.equal(startedRecord.startedAt)
    new Date(endedRecord.endedAt ?? '').should.deep.equal(終了時刻)
    endedRecord.should.have.property('endedAt', 終了時刻.toISOString())
  })

  test('記録を二件開始すると異なる識別子が発行されます', async () => {
    const secondRecord = await prepareStartedRecord(app)

    secondRecord.id.should.not.equal(startedRecord.id)
  })

  test('二件の記録を開始した後に一覧を取得すると開始した順に両方が返されます', async () => {
    const secondRecord = await prepareStartedRecord(app)

    const response = await app.inject({ method: 'GET', url: '/api/time-records', headers: { authorization: authorization() } })

    response.statusCode.should.equal(200)
    const content = { body: response.json() }
    content.should.have.property('body').that.deep.equal([startedRecord, secondRecord])
  })

  test('記録を終了した後に一覧を取得するとその記録の終了時刻が反映されます', async () => {
    await prepareEndedRecord(app, startedRecord.id)

    const response = await app.inject({ method: 'GET', url: '/api/time-records', headers: { authorization: authorization() } })

    response.statusCode.should.equal(200)
    const content = { body: response.json() }
    content.should.have.property('body').that.deep.equal([{ ...startedRecord, endedAt: 終了時刻.toISOString() }])
  })

  test.each(同一オリジンを示さないヘッダー値)(
    'Sec-Fetch-Siteが同一オリジンを示さない終了要求では403が返されます（%s）',
    async (secFetchSite) => {
      const response = await postWithFetchSite(app, `/api/time-records/${startedRecord.id}/end`, secFetchSite)

      response.statusCode.should.equal(403)
    },
  )

  test.each(同一オリジンを示さないヘッダー値)(
    'Sec-Fetch-Siteが同一オリジンを示さない終了要求（%s）を送っても、対象の記録は記録中のままです',
    async (secFetchSite) => {
      await postWithFetchSite(app, `/api/time-records/${startedRecord.id}/end`, secFetchSite)

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

  test('他の利用者の記録の終了を要求すると404が返されます', async () => {
    const response = await postFromSameOrigin(app, `/api/time-records/${startedRecord.id}/end`, 'second-user')

    response.statusCode.should.equal(404)
  })

  test('他の利用者が終了を要求しても、所有者の一覧には元の記録が残ります', async () => {
    await postFromSameOrigin(app, `/api/time-records/${startedRecord.id}/end`, 'second-user')

    const records = await readTimeRecords(app, 'first-user')
    records.should.deep.equal([startedRecord])
  })
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
  new Date(startedRecord.startedAt).should.deep.equal(開始時刻)
  startedRecord.startedAt.should.equal(開始時刻.toISOString())
  startedRecord.should.have.property('endedAt', null)
})

test('まだ記録を開始していない利用者が一覧を取得すると空の配列が返されます', async () => {
  const response = await app.inject({ method: 'GET', url: '/api/time-records', headers: { authorization: authorization() } })

  response.statusCode.should.equal(200)
  const content = { body: response.json() }
  content.should.have.property('body').that.deep.equal([])
})

test('同じ利用者でも、別に起動したアプリの一覧には元のアプリで開始した記録は含まれません', async () => {
  const startResponse = await postFromSameOrigin(app, '/api/time-records/start')
  startResponse.statusCode.should.equal(201)
  const independentApp = createTestApp()
  try {
    const response = await independentApp.inject({ method: 'GET', url: '/api/time-records', headers: { authorization: authorization() } })

    response.statusCode.should.equal(200)
    const content = { body: response.json() }
    content.should.have.property('body').that.deep.equal([])
  } finally {
    await independentApp.close()
  }
})

test.each(同一オリジンを示さないヘッダー値)(
  'Sec-Fetch-Siteが同一オリジンを示さない開始要求では403が返されます（%s）',
  async (secFetchSite) => {
    const response = await postWithFetchSite(app, '/api/time-records/start', secFetchSite)

    response.statusCode.should.equal(403)
  },
)

test.each(同一オリジンを示さないヘッダー値)(
  'Sec-Fetch-Siteが同一オリジンを示さない開始要求では記録が追加されません（%s）',
  async (secFetchSite) => {
    await postWithFetchSite(app, '/api/time-records/start', secFetchSite)

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
