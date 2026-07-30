import { useEffect, useState } from 'react'
import type { HealthApi } from './healthApi'
import { healthApi } from './healthApi'
import './index.css'

type AppProps = { api?: HealthApi }

export default function App({ api = healthApi }: AppProps) {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null)
  useEffect(() => {
    let active = true
    api.checkHealth().then((result) => active && setIsHealthy(result), () => active && setIsHealthy(false))
    return () => { active = false }
  }, [api])

  return <main><h1>Kizamu</h1><p>プロジェクトの準備が完了しました。</p><p aria-live="polite">サーバー接続: {isHealthy === null ? '確認中' : isHealthy ? '正常' : '失敗'}</p></main>
}
