export type HealthApi = { checkHealth: () => Promise<boolean> }

export const healthApi: HealthApi = {
  async checkHealth() {
    const response = await fetch('/api/health')
    if (!response.ok) return false
    const body: unknown = await response.json()
    return isHealthResponse(body) && body.status === 'ok'
  },
}

function isHealthResponse(value: unknown): value is { status: string } {
  return typeof value === 'object' && value !== null && 'status' in value
}
