import { render, screen } from '@testing-library/react'
import { describe, it } from 'vitest'
import HealthStatus from '../../../../src/frontends/react/src/health/HealthStatus'

describe('HealthStatus', () => {
  it('サーバーの応答を待つ間、通知領域には「サーバー接続: 確認中」と表示されます', () => {
    render(<HealthStatus health={{ checkHealth: () => new Promise(() => {}) }} />)

    screen.getByText('サーバー接続: 確認中').should.toHaveAttribute('aria-live', 'polite')
  })

  it.each([
    { isHealthy: true, status: '正常' },
    { isHealthy: false, status: '失敗' },
  ])('接続確認の結果を「サーバー接続: $status」と表示します', async ({ isHealthy, status }) => {
    render(<HealthStatus health={{ checkHealth: async () => isHealthy }} />)

    const connectionStatus = await screen.findByText(`サーバー接続: ${status}`)
    connectionStatus.should.toBeInTheDocument()
  })
})
