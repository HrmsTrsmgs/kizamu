import { useEffect, useState } from 'react'
import type { StartTimeRecordResponse, TimeRecordApi } from './timeRecordApi'

type TimeRecorderProps = {
  timeRecords: TimeRecordApi
}

export default function TimeRecorder({ timeRecords }: TimeRecorderProps) {
  const [records, setRecords] = useState<StartTimeRecordResponse[]>([])
  const [timeRecordId, setTimeRecordId] = useState<string | null>(null)
  const [startedAt, setStartedAt] = useState<string | null>(null)
  const [endedAt, setEndedAt] = useState<string | null>(null)

  useEffect(() => {
    async function getTimeRecords() {
      setRecords(await timeRecords.getTimeRecords())
    }

    void getTimeRecords()
  }, [timeRecords])

  async function startTimeRecord() {
    const record = await timeRecords.startTimeRecord()
    setRecords(currentRecords => [...currentRecords, record])
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
      <ul aria-label="作業記録一覧">
        {records.map(record => (
          <li key={record.id}>
            <time dateTime={record.startedAt}>{record.startedAt}</time>
          </li>
        ))}
      </ul>
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
