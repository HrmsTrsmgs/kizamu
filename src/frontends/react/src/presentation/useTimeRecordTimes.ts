import { useEffect, useReducer } from 'react'
import { Temporal } from 'temporal-polyfill'
import type { RecordTimestamp } from './timeRecordPresentation'
import { calculateTimeRecordDuration } from 'kizamu-domain-typescript'

const dateTimeFormatter = new Intl.DateTimeFormat('ja-JP', {
  timeZone: 'Asia/Tokyo',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

export function useTimeRecordTimes(startedAt: RecordTimestamp, endedAt: RecordTimestamp | null) {
  const [, updateCurrentTime] = useReducer(count => count + 1, 0)

  useEffect(() => {
    if (endedAt !== null) return
    const intervalId = setInterval(() => updateCurrentTime(), 1_000)
    return () => clearInterval(intervalId)
  }, [endedAt])

  const duration = calculateTimeRecordDuration(
    startedAt.instant,
    endedAt === null ? Temporal.Now.instant() : endedAt.instant,
  )

  return {
    startedAtText: dateTimeFormatter.format(startedAt.instant.epochMilliseconds),
    endedAtText: endedAt === null ? null : dateTimeFormatter.format(endedAt.instant.epochMilliseconds),
    durationText: `${duration.hours}時間${duration.minutes}分`,
  }
}
