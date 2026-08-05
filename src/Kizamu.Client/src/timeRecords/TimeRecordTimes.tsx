type TimeRecordTimesProps = {
  startedAt: string
  endedAt: string | null
  labelled?: boolean
}

const dateTimeFormatter = new Intl.DateTimeFormat('ja-JP', {
  timeZone: 'Asia/Tokyo',
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

export default function TimeRecordTimes({
  startedAt,
  endedAt,
  labelled = false,
}: TimeRecordTimesProps) {
  return (
    <div className="time-record-times">
      <div className="record-time">
        <span>開始</span>
        <time aria-label={labelled ? '開始時刻' : undefined} dateTime={startedAt}>
          {dateTimeFormatter.format(new Date(startedAt))}
        </time>
      </div>
      <div className="record-time">
        <span>終了</span>
        {endedAt !== null ? (
          <time aria-label={labelled ? '終了時刻' : undefined} dateTime={endedAt}>
            {dateTimeFormatter.format(new Date(endedAt))}
          </time>
        ) : (
          <span className="recording-label">記録中</span>
        )}
      </div>
    </div>
  )
}
