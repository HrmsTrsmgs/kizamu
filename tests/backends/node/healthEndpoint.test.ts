import { expect, test } from 'vitest'
import { createApp } from '../../../src/backends/node/app.js'

test('正常性を確認すると正常な状態が返されます', async () => {
  const app = createApp()
  try {
    const response = await app.inject({ method: 'GET', url: '/api/health' })

    expect(response.statusCode).toBe(200)
    expect(response.headers['content-type']).toContain('application/json')
    expect(response.json()).toEqual({ status: 'ok' })
  } finally {
    await app.close()
  }
})
