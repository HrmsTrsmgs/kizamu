import { useEffect, useState } from 'react'
import type { HealthApi } from './healthApi'
import { healthApi } from './healthApi'
import type { TimeRecordApi } from './timeRecordApi'
import { timeRecordApi } from './timeRecordApi'
import './index.css'

type AppProps = {
  health?: HealthApi
  timeRecords?: TimeRecordApi
}

export default function App({ health = healthApi, timeRecords = timeRecordApi }: AppProps) {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null)
  useEffect(() => {
    let active = true
    health.checkHealth().then((result) => active && setIsHealthy(result), () => active && setIsHealthy(false))
    return () => { active = false }
  }, [health])

  return <main><h1>Kizamu</h1><p>プロジェクトの準備が完了しました。</p><button type="button" onClick={() => { void timeRecords.startTimeRecord() }}>記録を開始</button><p aria-live="polite">サーバー接続: {isHealthy === null ? '確認中' : isHealthy ? '正常' : '失敗'}</p></main>
}
