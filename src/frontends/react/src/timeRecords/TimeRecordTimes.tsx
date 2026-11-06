import type { RecordTimestamp } from '../presentation/timeRecordPresentation'
import { useTimeRecordTimes } from '../presentation/useTimeRecordTimes'

type TimeRecordTimesProps = {
  startedAt: RecordTimestamp
  endedAt: RecordTimestamp | null
  labelled?: boolean
}

export default function TimeRecordTimes({
  startedAt,
  endedAt,
  labelled = false,
}: TimeRecordTimesProps) {
  const times = useTimeRecordTimes(startedAt, endedAt)

  return (
    <div className="time-record-times">
      <div className="record-time">
        <span>開始</span>
        <time aria-label={labelled ? '開始時刻' : undefined} dateTime={startedAt.isoText}>
          {times.startedAtText}
        </time>
      </div>
      <div className="record-time">
        <span>終了</span>
        {endedAt !== null ? (
          <time aria-label={labelled ? '終了時刻' : undefined} dateTime={endedAt.isoText}>
            {times.endedAtText}
          </time>
        ) : (
          <span className="recording-label">記録中</span>
        )}
      </div>
      <div className="record-time">
        <span>作業時間</span>
        <span className="time-record-duration-value">
          {times.durationText}
        </span>
      </div>
    </div>
  )
}
