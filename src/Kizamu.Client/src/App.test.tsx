import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import type { HealthApi } from './healthApi'

describe('App', () => {
  it('準備完了画面と正常な接続状態を表示する', async () => {
    const health: HealthApi = { checkHealth: async () => true }
    render(<App health={health} />)
    expect(screen.getByRole('heading', { name: 'Kizamu' })).toBeInTheDocument()
    expect(screen.getByText('プロジェクトの準備が完了しました。')).toBeInTheDocument()
    expect(await screen.findByText('サーバー接続: 正常')).toBeInTheDocument()
    expect(screen.queryByText(/count is/i)).not.toBeInTheDocument()
  })

  it('API失敗時に失敗を表示する', async () => {
    const health: HealthApi = { checkHealth: async () => { throw new Error('offline') } }
    render(<App health={health} />)
    expect(await screen.findByText('サーバー接続: 失敗')).toBeInTheDocument()
  })

  it('開始ボタンを押すと記録開始を要求します', () => {
    const startTimeRecord = vi.fn(async () => ({
      id: '019cde5b-4c00-7000-8000-000000000001',
      startedAt: '2026-08-01T09:00:00Z',
    }))
    const health = { checkHealth: async () => true }
    const timeRecords = { startTimeRecord }
    render(<App health={health} timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    expect(startTimeRecord).toHaveBeenCalledOnce()
  })
})
