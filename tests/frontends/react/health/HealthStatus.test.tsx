import { render, screen } from '@testing-library/react'
import { describe, it } from 'vitest'
import HealthStatus from '../../../../src/frontends/react/src/health/HealthStatus'

describe('HealthStatus', () => {
  it('確認中の接続状態を通知領域に表示します', () => {
    render(<HealthStatus health={{ checkHealth: () => new Promise(() => {}) }} />)

    screen.getByText('サーバー接続: 確認中').should.toHaveAttribute('aria-live', 'polite')
  })

  it.each([
    { isHealthy: true, status: '正常' },
    { isHealthy: false, status: '失敗' },
  ])('接続状態を日本語で表示します: $status', async ({ isHealthy, status }) => {
    render(<HealthStatus health={{ checkHealth: async () => isHealthy }} />)

    const connectionStatus = await screen.findByText(`サーバー接続: ${status}`)
    connectionStatus.should.toBeInTheDocument()
  })
})
