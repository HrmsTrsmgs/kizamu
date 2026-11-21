import { fileURLToPath } from 'node:url'
import { ESLint } from 'eslint'
import { beforeAll, describe, it } from 'vitest'

const lint = new ESLint({ cwd: fileURLToPath(new URL('../../../src/domains/typescript', import.meta.url)) })
const lintOptions = { filePath: 'example.ts' }

describe('共有Domainに適用するESLintの依存制約', () => {
  beforeAll(async () => {
    await lint.lintText('export {}', lintOptions)
  }, 30_000)

  it('フロントエンドへの型専用importを検出します', async () => {
    const rules = await reportedRules("import type { TimeRecordService } from '../../frontends/react/src/presentation/timeRecordService'")

    rules.should.include('no-restricted-imports')
  })

  it('Reactからの再エクスポートを検出します', async () => {
    const rules = await reportedRules("export { useState } from 'react'")

    rules.should.include('no-restricted-imports')
  })

  it('Node固有APIへの未使用のimportを検出します', async () => {
    const rules = await reportedRules("import { randomUUID } from 'node:crypto'")

    rules.should.include('no-restricted-imports')
  })

  it('Fastifyへのimportを検出します', async () => {
    const rules = await reportedRules("import Fastify from 'fastify'")

    rules.should.include('no-restricted-imports')
  })

  it('型importによるReactへの依存を検出します', async () => {
    const rules = await reportedRules("export type Example = import('react').ReactNode")

    rules.filter(rule => rule?.startsWith('no-restricted')).should.not.deep.equal([])
  })

  it('fetchによる直接通信を検出します', async () => {
    const rules = await reportedRules("void fetch('/api/time-records')")

    rules.should.include('no-restricted-globals')
  })

  it('バックエンドの動的importを検出します', async () => {
    const rules = await reportedRules("void import('../../backends/node/app.js')")

    rules.should.include('no-restricted-syntax')
  })

  it('内部モジュールの参照を許可します', async () => {
    const [report] = await lint.lintText("export type { TimeRecord } from './timeRecord.js'", lintOptions)

    report.messages.should.deep.equal([])
  })

  it('共有Domainから日時ライブラリtemporal-polyfillの型を参照できます', async () => {
    const [report] = await lint.lintText("export type { Temporal } from 'temporal-polyfill'", lintOptions)

    report.messages.should.deep.equal([])
  })
})

async function reportedRules(code: string) {
  const [report] = await lint.lintText(code, lintOptions)
  return report.messages.map(message => message.ruleId)
}
