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
      <h1>Kizamu</h1>
      <p>プロジェクトの準備が完了しました。</p>
      <TimeRecorder timeRecords={timeRecords} />
      <HealthStatus health={health} />
    </main>
  )
}
