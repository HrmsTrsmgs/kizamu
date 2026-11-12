import type { HealthService } from '../presentation/healthService'

export const healthService: HealthService = {
  async checkHealth() {
    const response = await fetch('/api/health')
    if (!response.ok) return false
    return hasHealthyStatus(await response.json())
  },
}

function hasHealthyStatus(body: unknown): boolean {
  return typeof body === 'object' && body !== null && 'status' in body && body.status === 'ok'
}
