import type { FastifyInstance } from 'fastify'
import Fastify from 'fastify'
import { registerTimeRecordEndpoints } from './api/timeRecordEndpoints.js'
import { TimeRecords } from './application/timeRecords.js'
import { timeRecordSources } from './infrastructure/timeRecordSources.js'
import { GoogleIdTokenVerifier } from './infrastructure/googleIdTokenVerifier.js'
import { InMemoryTimeRecordState } from './infrastructure/timeRecordState.js'

const healthyResponse = { status: 'ok' } as const

export function createApp(verifier = new GoogleIdTokenVerifier(process.env.GOOGLE_CLIENT_ID ?? '')): FastifyInstance {
  const app = Fastify()
  const timeRecords = new TimeRecords(new InMemoryTimeRecordState(), timeRecordSources)

  app.get<{ Reply: typeof healthyResponse }>('/api/health', async () => healthyResponse)
  registerTimeRecordEndpoints(app, timeRecords, verifier)
  return app
}
