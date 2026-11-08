import type { FastifyInstance } from 'fastify'
import Fastify from 'fastify'
import { registerTimeRecordEndpoints } from './api/timeRecordEndpoints.js'
import { TimeRecords } from './application/timeRecords.js'
import { timeRecordSources } from './infrastructure/timeRecordSources.js'
import { InMemoryTimeRecordState } from './infrastructure/timeRecordState.js'

export function createApp(): FastifyInstance {
  const app = Fastify()
  const timeRecords = new TimeRecords(new InMemoryTimeRecordState(), timeRecordSources)

  app.get('/api/health', async () => ({ status: 'ok' }))
  registerTimeRecordEndpoints(app, timeRecords)
  return app
}
