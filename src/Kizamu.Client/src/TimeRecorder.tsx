import { useState } from 'react'
import type { TimeRecordApi } from './timeRecordApi'

type TimeRecorderProps = {
  timeRecords: TimeRecordApi
}

export default function TimeRecorder({ timeRecords }: TimeRecorderProps) {
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
    <>
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
    </>
  )
}
