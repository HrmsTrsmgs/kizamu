import type { FastifyInstance } from 'fastify'
import type { TimeRecord } from 'kizamu-domain-typescript'
import type { GoogleIdTokenVerifier } from '../infrastructure/googleIdTokenVerifier.js'
import type { TimeRecords } from '../application/timeRecords.js'

export function registerTimeRecordEndpoints(app: FastifyInstance, timeRecords: TimeRecords, verifier: GoogleIdTokenVerifier): void {
  const owners = new Map<string, string>()
  app.register(async (sameOriginApp) => {
    sameOriginApp.addHook('preHandler', async (request, reply) => {
      if (request.headers['sec-fetch-site'] !== 'same-origin') return reply.code(403).send()
    })
    sameOriginApp.post('/api/time-records/start', async (request, reply) => {
      const authorization = request.headers.authorization
      const owner = authorization?.startsWith('Bearer ') ? await verifier.verify(authorization.slice(7)) : null
      if (owner === null) return reply.code(401).send()
      const record = timeRecords.start()
      owners.set(record.id, owner)
      return reply.code(201).send(toResponse(record))
    })
    sameOriginApp.post<{ Params: { id: string } }>('/api/time-records/:id/end', async (request) => {
      const record = timeRecords.end(request.params.id)
      if (record !== undefined) return toResponse(record)
    })
  })
  app.get('/api/time-records', async (request, reply) => {
    const authorization = request.headers.authorization
    const owner = authorization?.startsWith('Bearer ') ? await verifier.verify(authorization.slice(7)) : null
    if (owner === null) return reply.code(401).send()
    return timeRecords.list().filter(record => owners.get(record.id) === owner).map(toResponse)
  })
}

function toResponse(record: TimeRecord) {
  return {
    id: record.id,
    startedAt: record.startedAt.toString({ fractionalSecondDigits: 3 }),
    endedAt: record.endedAt?.toString({ fractionalSecondDigits: 3 }) ?? null,
  }
}
