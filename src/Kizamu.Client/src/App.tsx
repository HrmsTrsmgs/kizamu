import HealthStatus from './health/HealthStatus'
import TimeRecorder from './TimeRecorder'
import type { HealthApi } from './health/healthApi'
import { healthApi } from './health/healthApi'
import type { TimeRecordApi } from './timeRecordApi'
import { timeRecordApi } from './timeRecordApi'
import './index.css'

type AppProps = {
  health?: HealthApi
  timeRecords?: TimeRecordApi
}

export default function App({ health = healthApi, timeRecords = timeRecordApi }: AppProps) {
  return (
    <main>
      <h1>Kizamu</h1>
      <p>プロジェクトの準備が完了しました。</p>
      <TimeRecorder timeRecords={timeRecords} />
      <HealthStatus health={health} />
    </main>
  )
}
