import { useState } from 'react'
import HealthStatus from './HealthStatus'
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
  const [timeRecordId, setTimeRecordId] = useState<string | null>(null)
  const [startedAt, setStartedAt] = useState<string | null>(null)
  const [endedAt, setEndedAt] = useState<string | null>(null)

  async function startTimeRecord() {
    const record = await timeRecords.startTimeRecord()
    setTimeRecordId(record.id)
    setStartedAt(record.startedAt)
    setEndedAt(null)
  }

  async function endTimeRecord() {
    if (timeRecordId !== null) {
      const record = await timeRecords.endTimeRecord(timeRecordId)
      setEndedAt(record.endedAt)
      setTimeRecordId(null)
    }
  }

  return (
    <main>
      <h1>Kizamu</h1>
      <p>プロジェクトの準備が完了しました。</p>
      {timeRecordId === null ? (
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
      {endedAt !== null && (
        <time aria-label="終了時刻" dateTime={endedAt}>
          {endedAt}
        </time>
      )}
      <HealthStatus health={health} />
    </main>
  )
}
