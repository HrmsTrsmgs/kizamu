import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { createApp } from '../../../src/backends/node/app.js'

interface TimeRecordResponse {
  id: string
  startedAt: string
  endedAt: string | null
}

const 開始時刻 = new Date('2026-08-01T09:00:00.000Z')
const 終了時刻 = new Date('2026-08-01T10:30:00.000Z')
let app: ReturnType<typeof createApp>

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(開始時刻)
  app = createApp()
})

afterEach(async () => {
  await app.close()
  vi.useRealTimers()
})

test('記録を開始すると空でないGUIDとサーバーの開始時刻があり終了時刻がない記録が返されます', async () => {
  const response = await app.inject({ method: 'POST', url: '/api/time-records/start' })

  expect(response.statusCode).toBe(201)
  const startedRecord = response.json<TimeRecordResponse>()
  expect(startedRecord.id).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)
  expect(startedRecord.id).not.toBe('00000000-0000-0000-0000-000000000000')
  expect(new Date(startedRecord.startedAt)).toEqual(開始時刻)
  expect(startedRecord.endedAt).toBeNull()
})

test('記録を終了すると同じ識別子と元の開始時刻とサーバーの終了時刻が返されます', async () => {
  const startResponse = await app.inject({ method: 'POST', url: '/api/time-records/start' })
  expect(startResponse.statusCode).toBe(201)
  const startedRecord = startResponse.json<TimeRecordResponse>()
  vi.setSystemTime(終了時刻)

  const response = await app.inject({ method: 'POST', url: `/api/time-records/${startedRecord.id}/end` })

  expect(response.statusCode).toBe(200)
  const endedRecord = response.json<TimeRecordResponse>()
  expect(endedRecord.id).toBe(startedRecord.id)
  expect(endedRecord.startedAt).toBe(startedRecord.startedAt)
  expect(new Date(endedRecord.endedAt ?? '')).toEqual(終了時刻)
})

test('まだ開始していないときに一覧を取得すると空の配列が返されます', async () => {
  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  expect(response.statusCode).toBe(200)
  expect(response.json()).toEqual([])
})

test('二件の記録を開始した後に一覧を取得すると開始した順に両方が返されます', async () => {
  const firstStartResponse = await app.inject({ method: 'POST', url: '/api/time-records/start' })
  expect(firstStartResponse.statusCode).toBe(201)
  const firstRecord = firstStartResponse.json<TimeRecordResponse>()
  vi.setSystemTime(終了時刻)
  const secondStartResponse = await app.inject({ method: 'POST', url: '/api/time-records/start' })
  expect(secondStartResponse.statusCode).toBe(201)
  const secondRecord = secondStartResponse.json<TimeRecordResponse>()
  expect(secondRecord.id).not.toBe(firstRecord.id)

  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  expect(response.statusCode).toBe(200)
  expect(response.json()).toEqual([firstRecord, secondRecord])
})

test('記録を終了した後に一覧を取得するとその記録の終了時刻が反映されます', async () => {
  const startResponse = await app.inject({ method: 'POST', url: '/api/time-records/start' })
  expect(startResponse.statusCode).toBe(201)
  const startedRecord = startResponse.json<TimeRecordResponse>()
  vi.setSystemTime(終了時刻)
  const endResponse = await app.inject({ method: 'POST', url: `/api/time-records/${startedRecord.id}/end` })
  expect(endResponse.statusCode).toBe(200)

  const response = await app.inject({ method: 'GET', url: '/api/time-records' })

  expect(response.statusCode).toBe(200)
  expect(response.json()).toEqual([{ ...startedRecord, endedAt: 終了時刻.toISOString() }])
})

test('別のアプリで開始した記録は新しく生成したアプリの一覧には含まれません', async () => {
  const startResponse = await app.inject({ method: 'POST', url: '/api/time-records/start' })
  expect(startResponse.statusCode).toBe(201)
  const independentApp = createApp()
  try {
    const response = await independentApp.inject({ method: 'GET', url: '/api/time-records' })

    expect(response.statusCode).toBe(200)
    expect(response.json()).toEqual([])
  } finally {
    await independentApp.close()
  }
})
