// @vitest-environment node
import { fileURLToPath } from 'node:url'
import { ESLint } from 'eslint'
import { beforeAll, describe, it } from 'vitest'

const lint = new ESLint({ cwd: fileURLToPath(new URL('../../../../src/frontends/react', import.meta.url)) })

describe('Reactの依存境界', () => {
  beforeAll(async () => {
    // Load the TypeScript/React lint configuration before timing individual boundary checks.
    await lint.lintText('export {}', { filePath: 'src/presentation/example.ts' })
  }, 30_000)

  it.each([
    { file: 'src/presentation/example.ts', code: "import { timeRecordService } from '../infrastructure/timeRecordService'" },
    { file: 'src/presentation/example.ts', code: "import TimeRecorder from '../timeRecords/TimeRecorder'" },
    { file: 'src/infrastructure/example.ts', code: "import HealthStatus from '../health/HealthStatus'" },
    { file: 'src/timeRecords/Example.tsx', code: "import { timeRecordService } from '../infrastructure/timeRecordService'" },
  ])('未使用と型専用を含め禁止方向の静的importと再exportを検出します: $file $code', async ({ file, code }) => {
    const [report] = await lint.lintText(code, { filePath: file })

    report.messages.map(message => message.ruleId).should.include('no-restricted-imports')
  })

  it.each([
    { file: 'src/presentation/example.ts', code: "void fetch('/api/time-records')", rule: 'no-restricted-globals' },
    { file: 'src/presentation/example.ts', code: "void window.fetch('/api/time-records')", rule: 'no-restricted-globals' },
    { file: 'src/presentation/example.ts', code: "void import('../infrastructure/timeRecordService')", rule: 'no-restricted-syntax' },
  ])('内側への通信と動的importの混入を検出します: $code', async ({ file, code, rule }) => {
    const [report] = await lint.lintText(code, { filePath: file })

    report.messages.map(message => message.ruleId).should.include(rule)
  })

  it.each([
    { file: 'src/presentation/example.ts', code: "import type { TimeRecord } from 'kizamu-domain-typescript'; export type Record = TimeRecord" },
    { file: 'src/infrastructure/example.ts', code: "import type { TimeRecordService } from '../presentation/timeRecordService'; export type Service = TimeRecordService" },
    { file: 'src/timeRecords/Example.tsx', code: "export { useTimeRecorder } from '../presentation/useTimeRecorder'" },
    { file: 'src/main.tsx', code: "export { timeRecordService } from './infrastructure/timeRecordService'" },
  ])('許可した方向の依存は妨げません: $file', async ({ file, code }) => {
    const [report] = await lint.lintText(code, { filePath: file })

    report.messages.filter(message => message.ruleId?.startsWith('no-restricted')).should.deep.equal([])
  })
})
