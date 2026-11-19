import { useEffect, useReducer } from 'react'
import { Temporal } from 'temporal-polyfill'
import type { TimeRecord } from 'kizamu-domain-typescript'

const dateTimeFormatter = new Intl.DateTimeFormat('ja-JP', {
  timeZone: 'Asia/Tokyo',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

export function useTimeRecordTimes(record: TimeRecord) {
  const { startedAt, endedAt } = record
  const [, updateCurrentTime] = useReducer(count => count + 1, 0)

  useEffect(() => {
    if (endedAt !== null) return
    const intervalId = setInterval(() => updateCurrentTime(), 1_000)
    return () => clearInterval(intervalId)
  }, [endedAt])

  const duration = record.durationAt(endedAt ?? Temporal.Now.instant())

  return {
    startedAtText: dateTimeFormatter.format(startedAt.epochMilliseconds),
    endedAtText: endedAt === null ? null : dateTimeFormatter.format(endedAt.epochMilliseconds),
    durationText: `${duration.hours}時間${duration.minutes}分`,
  }
}
