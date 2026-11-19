// @vitest-environment node
import { fileURLToPath } from 'node:url'
import { ESLint } from 'eslint'
import { beforeAll, describe, it } from 'vitest'

const lint = new ESLint({ cwd: fileURLToPath(new URL('../../../../src/frontends/react', import.meta.url)) })

describe('Reactに適用するESLintの依存制約', () => {
  beforeAll(async () => {
    // Load the TypeScript/React lint configuration before timing individual boundary checks.
    await lint.lintText('export {}', { filePath: 'src/presentation/example.ts' })
  }, 30_000)

  it.each([
    { from: 'Presentation', to: 'Infrastructure', file: 'src/presentation/example.ts', code: "import { timeRecordService } from '../infrastructure/timeRecordService'" },
    { from: 'Presentation', to: '画面', file: 'src/presentation/example.ts', code: "import TimeRecorder from '../timeRecords/TimeRecorder'" },
    { from: 'Presentation', to: '画面', file: 'src/presentation/example.ts', code: "export { default } from '../App.js'" },
    { from: 'Infrastructure', to: '画面', file: 'src/infrastructure/example.ts', code: "import HealthStatus from '../health/HealthStatus'" },
    { from: '画面', to: 'Infrastructure', file: 'src/timeRecords/Example.tsx', code: "import { timeRecordService } from '../infrastructure/timeRecordService'" },
  ])('$from から $to への静的参照（$code）を依存違反として検出します', async ({ file, code }) => {
    const [report] = await lint.lintText(code, { filePath: file })

    report.messages.map(message => message.ruleId).should.include('no-restricted-imports')
  })

  it.each([
    "void fetch('/api/time-records')",
    "void window.fetch('/api/time-records')",
  ])('Presentationでfetchを使った直接通信（%s）を依存違反として検出します', async code => {
    const [report] = await lint.lintText(code, { filePath: 'src/presentation/example.ts' })

    report.messages.map(message => message.ruleId).should.include('no-restricted-globals')
  })

  it('PresentationからInfrastructureを動的にimportすると依存違反になります', async () => {
    const [report] = await lint.lintText("void import('../infrastructure/timeRecordService')", { filePath: 'src/presentation/example.ts' })

    report.messages.map(message => message.ruleId).should.include('no-restricted-syntax')
  })

  it.each([
    { from: 'Presentation', to: 'Domain', file: 'src/presentation/example.ts', code: "import type { TimeRecord } from 'kizamu-domain-typescript'; export type Record = TimeRecord" },
    { from: 'Infrastructure', to: 'Presentation', file: 'src/infrastructure/example.ts', code: "import type { TimeRecordService } from '../presentation/timeRecordService'; export type Service = TimeRecordService" },
    { from: '画面', to: 'Presentation', file: 'src/timeRecords/Example.tsx', code: "export { useTimeRecorder } from '../presentation/useTimeRecorder'" },
    { from: '起動処理', to: 'Infrastructure', file: 'src/main.tsx', code: "export { timeRecordService } from './infrastructure/timeRecordService'" },
  ])('$from から $to への静的参照（$code）を許可します', async ({ file, code }) => {
    const [report] = await lint.lintText(code, { filePath: file })

    report.messages.filter(message => message.ruleId?.startsWith('no-restricted')).should.deep.equal([])
  })
})
