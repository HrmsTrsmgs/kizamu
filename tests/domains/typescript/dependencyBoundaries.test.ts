import { fileURLToPath } from 'node:url'
import { ESLint } from 'eslint'
import { beforeAll, describe, it } from 'vitest'

const lint = new ESLint({ cwd: fileURLToPath(new URL('../../../src/domains/typescript', import.meta.url)) })

describe('共有Domainの依存境界', () => {
  beforeAll(async () => {
    await lint.lintText('export {}', { filePath: 'example.ts' })
  }, 30_000)

  it.each([
    "import type { TimeRecordService } from '../../frontends/react/src/presentation/timeRecordService'",
    "export { useState } from 'react'",
    "import { randomUUID } from 'node:crypto'",
    "import Fastify from 'fastify'",
  ])('未使用と型専用を含め内側から外側への依存を検出します: %s', async code => {
    const [report] = await lint.lintText(code, { filePath: 'example.ts' })

    report.messages.map(message => message.ruleId).should.include('no-restricted-imports')
  })

  it.each([
    { code: "void fetch('/api/time-records')", rule: 'no-restricted-globals' },
    { code: "void import('../../backends/node/app.js')", rule: 'no-restricted-syntax' },
  ])('直接通信と動的importを検出します: $code', async ({ code, rule }) => {
    const [report] = await lint.lintText(code, { filePath: 'example.ts' })

    report.messages.map(message => message.ruleId).should.include(rule)
  })

  it.each([
    "export type { TimeRecord } from './timeRecord.js'",
    "export type { Temporal } from 'temporal-polyfill'",
  ])('内部モジュールと既存日時ライブラリの参照を許可します: %s', async code => {
    const [report] = await lint.lintText(code, { filePath: 'example.ts' })

    report.messages.filter(message => message.ruleId?.startsWith('no-restricted')).should.deep.equal([])
  })
})
