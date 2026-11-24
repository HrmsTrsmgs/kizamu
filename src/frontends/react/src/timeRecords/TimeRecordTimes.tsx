import type { TimeRecord } from 'kizamu-domain-typescript'
import { useTimeRecordTimes } from '../presentation/useTimeRecordTimes'
import { formatHtmlDateTime } from './htmlDateTime'

type TimeRecordTimesProps = {
  record: TimeRecord
  labelled?: boolean
}

export default function TimeRecordTimes({
  record,
  labelled = false,
}: TimeRecordTimesProps) {
  const times = useTimeRecordTimes(record)

  return (
    <div className="time-record-times">
      <div className="record-time">
        <span>開始</span>
        <time aria-label={labelled ? '開始時刻' : undefined} dateTime={formatHtmlDateTime(record.startedAt)}>
          {times.startedAtText}
        </time>
      </div>
      <div className="record-time">
        <span>終了</span>
        {record.endedAt !== null ? (
          <time aria-label={labelled ? '終了時刻' : undefined} dateTime={formatHtmlDateTime(record.endedAt)}>
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
