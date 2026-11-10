import { afterEach, describe, it, vi } from 'vitest'
import { healthService } from '../../../../src/frontends/react/src/infrastructure/healthService'

const tested = healthService

describe('healthService', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('正常性確認APIへ要求します', async () => {
    const fetchMock = vi.fn(async () => response({ status: 'ok' }))
    vi.stubGlobal('fetch', fetchMock)

    await tested.checkHealth()

    fetchMock.should.have.callCount(1)
    fetchMock.should.have.been.calledWith('/api/health')
  })

  it('正常なレスポンスを受け取るとtrueを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response({ status: 'ok' })))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(true)
  })

  it('正常でないHTTPレスポンスを受け取るとfalseを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 503 })))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(false)
  })

  it('正常でない内容を受け取るとfalseを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response({})))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(false)
  })
})

function response(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
  })
}
