import { useId } from 'react'
import type { TimeRecord } from 'kizamu-domain-typescript'
import TimeRecordTimes from './TimeRecordTimes'

export default function TimeRecordList({ records }: { records: readonly TimeRecord[] }) {
  const titleId = useId()

  return (
    <section className="records-panel" aria-labelledby={titleId}>
      <header className="panel-header records-header">
        <h2 id={titleId}>作業記録</h2>
        <span className="record-count">{records.length}件</span>
      </header>

      <ul className="records-list" aria-label="作業記録一覧">
        {records.map(record => (
          <li key={record.id}>
            <span className={`record-status ${record.endedAt === null ? 'is-active' : ''}`} aria-hidden="true" />
            <TimeRecordTimes record={record} />
          </li>
        ))}
      </ul>
    </section>
  )
}
