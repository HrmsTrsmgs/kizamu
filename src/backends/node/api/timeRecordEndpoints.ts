import type { FastifyInstance } from 'fastify'
import type { TimeRecord } from 'kizamu-domain-typescript'
import type { TimeRecords } from '../application/timeRecords.js'

export function registerTimeRecordEndpoints(app: FastifyInstance, timeRecords: TimeRecords): void {
  app.post('/api/time-records/start', async (request, reply) => {
    if (request.headers['sec-fetch-site'] !== 'same-origin') return reply.code(403).send()

    return reply.code(201).send(toResponse(timeRecords.start()))
  })
  app.post<{ Params: { id: string } }>('/api/time-records/:id/end', async (request, reply) => {
    if (request.headers['sec-fetch-site'] !== 'same-origin') return reply.code(403).send()

    const record = timeRecords.end(request.params.id)
    if (record !== undefined) return toResponse(record)
  })
  app.get('/api/time-records', async () => timeRecords.list().map(toResponse))
}

function toResponse(record: TimeRecord) {
  return {
    id: record.id,
    startedAt: record.startedAt.toString({ fractionalSecondDigits: 3 }),
    endedAt: record.endedAt?.toString({ fractionalSecondDigits: 3 }) ?? null,
  }
}
