import { afterEach, beforeEach, describe, it, vi, type Mock } from 'vitest'
import { healthService } from '../../../../src/frontends/react/src/infrastructure/healthService'

const tested = healthService

describe('healthService', () => {
  let fetchMock: Mock<typeof fetch>

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('正常性確認APIへ要求します', async () => {
    fetchMock.mockImplementation(async () => Response.json({ status: 'ok' }))

    await tested.checkHealth()

    fetchMock.should.have.been.calledOnceWith('/api/health')
  })

  it('正常なレスポンスを受け取るとtrueを返します', async () => {
    fetchMock.mockImplementation(async () => Response.json({ status: 'ok' }))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(true)
  })

  it('正常でないHTTPレスポンスを受け取るとfalseを返します', async () => {
    fetchMock.mockImplementation(async () => new Response(null, { status: 503 }))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(false)
  })

  it.each([{}, { status: 1 }])('正常でない内容を受け取るとfalseを返します（%j）', async body => {
    fetchMock.mockImplementation(async () => Response.json(body))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(false)
  })
})
