import HealthStatus from './health/HealthStatus'
import TimeRecorder from './timeRecords/TimeRecorder'
import type { HealthApi } from './health/healthApi'
import type { TimeRecordApi } from './timeRecords/timeRecordApi'
import './index.css'

type AppProps = {
  health: HealthApi
  timeRecords: TimeRecordApi
}

export default function App({ health, timeRecords }: AppProps) {
  return (
    <main>
      <header className="app-header">
        <h1>Kizamu</h1>
        <p className="app-description">作業時間を記録します。</p>
      </header>
      <TimeRecorder timeRecords={timeRecords} />
      <footer className="app-footer">
        <HealthStatus health={health} />
      </footer>
    </main>
  )
}
