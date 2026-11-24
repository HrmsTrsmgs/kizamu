import type { ComponentProps } from 'react'
import type { TimeRecord } from 'kizamu-domain-typescript'
import TimeRecorderPanel from './TimeRecorderPanel'
import TimeRecordList from './TimeRecordList'

type TimeRecorderViewProps = ComponentProps<typeof TimeRecorderPanel> & {
  records: readonly TimeRecord[]
}

export function TimeRecorderView({ records, ...panel }: TimeRecorderViewProps) {
  return (
    <div className="time-recorder">
      <TimeRecorderPanel {...panel} />
      <TimeRecordList records={records} />
    </div>
  )
}
