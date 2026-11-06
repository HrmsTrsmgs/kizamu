import type { HealthService } from '../presentation/healthService'
import { useHealthStatus } from '../presentation/useHealthStatus'

type HealthStatusProps = {
  health: HealthService
}

export default function HealthStatus({ health }: HealthStatusProps) {
  const isHealthy = useHealthStatus(health)

  let status = '確認中'
  if (isHealthy === true) {
    status = '正常'
  } else if (isHealthy === false) {
    status = '失敗'
  }

  return <p aria-live="polite">サーバー接続: {status}</p>
}
