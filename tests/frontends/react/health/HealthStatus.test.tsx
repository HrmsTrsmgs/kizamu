import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import HealthStatus from '../../../../src/frontends/react/src/health/HealthStatus'
import type { HealthApi } from '../../../../src/frontends/react/src/health/healthApi'

describe('HealthStatus', () => {
  it('正常な接続状態を表示します', async () => {
    const health: HealthApi = { checkHealth: async () => true }

    render(<HealthStatus health={health} />)

    expect(await screen.findByText('サーバー接続: 正常')).toBeInTheDocument()
  })

  it('APIに接続できない場合は失敗を表示します', async () => {
    const health: HealthApi = { checkHealth: async () => { throw new Error('offline') } }

    render(<HealthStatus health={health} />)

    expect(await screen.findByText('サーバー接続: 失敗')).toBeInTheDocument()
  })
})
