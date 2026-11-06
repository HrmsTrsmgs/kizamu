import { useEffect, useState } from 'react'
import type { HealthService } from './healthService'

export function useHealthStatus(health: HealthService) {
  const [isHealthy, setIsHealthy] = useState<boolean | null>(null)

  useEffect(() => {
    let active = true

    async function checkHealth() {
      try {
        const result = await health.checkHealth()
        if (active) setIsHealthy(result)
      } catch {
        if (active) setIsHealthy(false)
      }
    }

    void checkHealth()
    return () => { active = false }
  }, [health])

  return isHealthy
}
