import type { FastifyInstance } from 'fastify'
import type { TimeRecord } from 'kizamu-domain-typescript'
import type { GoogleIdTokenVerifier } from '../infrastructure/googleIdTokenVerifier.js'
import { TimeRecordNotFoundError } from '../application/timeRecords.js'
import type { TimeRecords } from '../application/timeRecords.js'

export function registerTimeRecordEndpoints(app: FastifyInstance, timeRecords: TimeRecords, verifier: GoogleIdTokenVerifier): void {
  const owners = new Map<string, string>()
  app.register(async (sameOriginApp) => {
    sameOriginApp.addHook('preHandler', async (request, reply) => {
      if (request.headers['sec-fetch-site'] !== 'same-origin') return reply.code(403).send()
    })
    sameOriginApp.post('/api/time-records/start', async (request, reply) => {
      const owner = await getOwner(request.headers.authorization, verifier)
      if (owner === null) return reply.code(401).send()
      const record = timeRecords.start()
      owners.set(record.id, owner)
      return reply.code(201).send(toResponse(record))
    })
    sameOriginApp.post<{ Params: { id: string } }>('/api/time-records/:id/end', async (request, reply) => {
      const owner = await getOwner(request.headers.authorization, verifier)
      if (owner === null) return reply.code(401).send()
      if (owners.get(request.params.id) !== owner) return reply.code(404).send()
      try {
        return toResponse(timeRecords.end(request.params.id))
      } catch (error) {
        if (error instanceof TimeRecordNotFoundError) return reply.code(404).send()
        throw error
      }
    })
  })
  app.get('/api/time-records', async (request, reply) => {
    const owner = await getOwner(request.headers.authorization, verifier)
    if (owner === null) return reply.code(401).send()
    return timeRecords.list().filter(record => owners.get(record.id) === owner).map(toResponse)
  })
}

function getOwner(authorization: string | undefined, verifier: GoogleIdTokenVerifier): Promise<string | null> {
  return authorization?.slice(0, 7).toLowerCase() === 'bearer ' ? verifier.verify(authorization.slice(7)) : Promise.resolve(null)
}

function toResponse(record: TimeRecord) {
  return {
    id: record.id,
    startedAt: record.startedAt.toString({ fractionalSecondDigits: 3 }),
    endedAt: record.endedAt?.toString({ fractionalSecondDigits: 3 }) ?? null,
  }
}
