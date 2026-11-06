import { test } from 'vitest'
import { createApp } from '../../../src/backends/node/app.js'

test('正常性を確認すると正常な状態が返されます', async () => {
  const app = createApp()
  try {
    const response = await app.inject({ method: 'GET', url: '/api/health' })

    response.statusCode.should.equal(200)
    response.headers.should.have.property('content-type').that.include('application/json')
    const content = { body: response.json() }
    content.should.have.property('body').that.deep.equal({ status: 'ok' })
  } finally {
    await app.close()
  }
})
