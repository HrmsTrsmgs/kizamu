import { afterEach, describe, expect, it, vi } from 'vitest'
import { healthApi } from './healthApi'

describe('healthApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('正常性確認APIへ要求します', async () => {
    const fetchMock = vi.fn(async () => response({ status: 'ok' }))
    vi.stubGlobal('fetch', fetchMock)

    await healthApi.checkHealth()

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith('/api/health')
  })

  it('正常なレスポンスを受け取るとtrueを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response({ status: 'ok' })))

    const result = await healthApi.checkHealth()

    expect(result).toBe(true)
  })

  it('正常でないHTTPレスポンスを受け取るとfalseを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 503 })))

    const result = await healthApi.checkHealth()

    expect(result).toBe(false)
  })

  it('正常でない内容を受け取るとfalseを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response({})))

    const result = await healthApi.checkHealth()

    expect(result).toBe(false)
  })
})

function response(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
  })
}
