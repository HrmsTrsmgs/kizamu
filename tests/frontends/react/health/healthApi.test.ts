import { afterEach, describe, expect, it, vi } from 'vitest'
import { healthApi } from '../../../../src/frontends/react/src/health/healthApi'

const tested = healthApi

describe('healthApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('正常性確認APIへ要求します', async () => {
    const fetchMock = vi.fn(async () => response({ status: 'ok' }))
    vi.stubGlobal('fetch', fetchMock)

    await tested.checkHealth()

    expect(fetchMock).toHaveBeenCalledOnce()
    expect(fetchMock).toHaveBeenCalledWith('/api/health')
  })

  it('正常なレスポンスを受け取るとtrueを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response({ status: 'ok' })))

    expect(await tested.checkHealth()).toBe(true)
  })

  it('正常でないHTTPレスポンスを受け取るとfalseを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 503 })))

    expect(await tested.checkHealth()).toBe(false)
  })

  it('正常でない内容を受け取るとfalseを返します', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => response({})))

    expect(await tested.checkHealth()).toBe(false)
  })
})

function response(body: unknown) {
  return new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
  })
}
