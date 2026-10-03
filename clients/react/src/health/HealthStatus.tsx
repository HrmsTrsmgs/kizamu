import { useEffect, useState } from 'react'
import type { HealthApi } from './healthApi'

type HealthStatusProps = {
  health: HealthApi
}

export default function HealthStatus({ health }: HealthStatusProps) {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null)

  useEffect(() => {
    let active = true

    async function checkHealth() {
      try {
        const result = await health.checkHealth()
        if (active) {
          setIsHealthy(result)
        }
      } catch {
        if (active) {
          setIsHealthy(false)
        }
      }
    }

    void checkHealth()

    return () => { active = false }
  }, [health])

  let status = '確認中'
  if (isHealthy === true) {
    status = '正常'
  } else if (isHealthy === false) {
    status = '失敗'
  }

  return <p aria-live="polite">サーバー接続: {status}</p>
}
