import type { FastifyInstance } from 'fastify'
import Fastify from 'fastify'
import { randomUUID } from 'node:crypto'
import { Temporal } from 'temporal-polyfill'
import { TimeRecord } from 'kizamu-domain-typescript'

export function createApp(): FastifyInstance {
  const app = Fastify()
  const timeRecords: TimeRecord[] = []

  app.get('/api/health', async () => ({ status: 'ok' }))
  app.post('/api/time-records/start', async (_request, reply) => {
    const timeRecord = TimeRecord.start(randomUUID(), Temporal.Instant.fromEpochMilliseconds(Date.now()))
    timeRecords.push(timeRecord)
    return reply.code(201).send(toResponse(timeRecord))
  })
  app.post<{ Params: { id: string } }>('/api/time-records/:id/end', async (request) => {
    const index = timeRecords.findIndex((record) => record.id === request.params.id)
    if (index !== -1) {
      const timeRecord = timeRecords[index].end(Temporal.Instant.fromEpochMilliseconds(Date.now()))
      timeRecords[index] = timeRecord
      return toResponse(timeRecord)
    }
  })
  app.get('/api/time-records', async () => timeRecords.map(toResponse))

  return app
}

function toResponse(record: TimeRecord) {
  return {
    id: record.id,
    startedAt: record.startedAt.toString({ fractionalSecondDigits: 3 }),
    endedAt: record.endedAt?.toString({ fractionalSecondDigits: 3 }) ?? null,
  }
}
