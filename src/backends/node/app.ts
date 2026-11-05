import type { FastifyInstance } from 'fastify'
import Fastify from 'fastify'
import { randomUUID } from 'node:crypto'

interface TimeRecord {
  id: string
  startedAt: string
  endedAt: string | null
}

export function createApp(): FastifyInstance {
  const app = Fastify()
  const timeRecords: TimeRecord[] = []

  app.get('/api/health', async () => ({ status: 'ok' }))
  app.post('/api/time-records/start', async (_request, reply) => {
    const timeRecord: TimeRecord = {
      id: randomUUID(),
      startedAt: new Date().toISOString(),
      endedAt: null,
    }
    timeRecords.push(timeRecord)
    return reply.code(201).send(timeRecord)
  })
  app.post<{ Params: { id: string } }>('/api/time-records/:id/end', async (request) => {
    const timeRecord = timeRecords.find((record) => record.id === request.params.id)
    if (timeRecord) {
      timeRecord.endedAt = new Date().toISOString()
    }
    return timeRecord
  })
  app.get('/api/time-records', async () => timeRecords)

  return app
}
