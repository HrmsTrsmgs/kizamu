import { test } from 'vitest'
import { createApp } from '../../../src/backends/node/app.js'

test('GET /api/health は HTTP 200 と status が ok の JSON を返します', async () => {
  const app = createApp()
  try {
    const response = await app.inject({ method: 'GET', url: '/api/health' })

    response.statusCode.should.equal(200)
    response.headers.should.have.property('content-type').that.include('application/json')
    response.json<{ status: string }>().should.deep.equal({ status: 'ok' })
  } finally {
    await app.close()
  }
})
