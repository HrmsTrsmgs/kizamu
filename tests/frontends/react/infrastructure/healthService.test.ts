import { afterEach, beforeEach, describe, it, vi, type Mock } from 'vitest'
import { healthService } from '../../../../src/frontends/react/src/infrastructure/healthService'

const tested = healthService

describe('healthService', () => {
  let fetchMock: Mock<typeof fetch>

  beforeEach(() => {
    fetchMock = vi.fn<typeof fetch>(async () => Response.json({ status: 'ok' }))
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('サーバーの接続確認では /api/health に要求を一度送ります', async () => {
    await tested.checkHealth()

    fetchMock.should.have.been.calledOnceWith('/api/health')
  })

  it('接続確認の応答でstatusがokならtrueを返します', async () => {
    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(true)
  })

  it('接続確認APIが503を返すと、接続確認の結果はfalseです', async () => {
    fetchMock.mockImplementation(async () => new Response(null, { status: 503 }))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(false)
  })

  it.each([{}, { status: 1 }])('接続確認APIの応答本文が %j なら、接続確認の結果はfalseです', async body => {
    fetchMock.mockImplementation(async () => Response.json(body))

    const isHealthy = await tested.checkHealth()
    isHealthy.should.equal(false)
  })
})
