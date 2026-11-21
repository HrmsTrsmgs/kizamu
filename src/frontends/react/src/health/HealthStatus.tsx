import type { HealthService } from '../presentation/healthService'
import { useHealthStatus } from '../presentation/useHealthStatus'

type HealthStatusProps = {
  health: HealthService
}

export default function HealthStatus({ health }: HealthStatusProps) {
  const isHealthy = useHealthStatus(health)

  let statusText: string
  switch (isHealthy) {
    case true:
      statusText = '正常'
      break
    case false:
      statusText = '失敗'
      break
    default:
      statusText = '確認中'
  }

  return <p aria-live="polite">サーバー接続: {statusText}</p>
}
