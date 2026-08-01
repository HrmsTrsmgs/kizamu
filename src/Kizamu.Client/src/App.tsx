import { useEffect, useState } from 'react'
import type { HealthApi } from './healthApi'
import { healthApi } from './healthApi'
import type { TimeRecordApi } from './timeRecordApi'
import { timeRecordApi } from './timeRecordApi'
import './index.css'

type AppProps = {
  health?: HealthApi
  timeRecords?: TimeRecordApi
}

export default function App({ health = healthApi, timeRecords = timeRecordApi }: AppProps) {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null)
  const [timeRecordId, setTimeRecordId] = useState<string | null>(null)
  const [startedAt, setStartedAt] = useState<string | null>(null)
  useEffect(() => {
    let active = true

    async function checkHealth() {
      try {
        const result = await health.checkHealth()
        if (active) {
          setIsHealthy(result)
        }
      } catch {
        if (active) {
          setIsHealthy(false)
        }
      }
    }

    void checkHealth()

    return () => { active = false }
  }, [health])

  async function startTimeRecord() {
    const record = await timeRecords.startTimeRecord()
    setTimeRecordId(record.id)
    setStartedAt(record.startedAt)
  }

  async function endTimeRecord() {
    if (timeRecordId !== null) {
      await timeRecords.endTimeRecord(timeRecordId)
    }
  }

  let healthStatus = '確認中'
  if (isHealthy === true) {
    healthStatus = '正常'
  } else if (isHealthy === false) {
    healthStatus = '失敗'
  }

  return (
    <main>
      <h1>Kizamu</h1>
      <p>プロジェクトの準備が完了しました。</p>
      {startedAt === null ? (
        <button type="button" onClick={() => { void startTimeRecord() }}>
          記録を開始
        </button>
      ) : (
        <button type="button" onClick={() => { void endTimeRecord() }}>
          記録を終了
        </button>
      )}
      {startedAt !== null && (
        <time aria-label="開始時刻" dateTime={startedAt}>
          {startedAt}
        </time>
      )}
      <p aria-live="polite">サーバー接続: {healthStatus}</p>
    </main>
  )
}
