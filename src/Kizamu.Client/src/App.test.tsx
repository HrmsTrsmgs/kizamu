import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'
import type { HealthApi } from './healthApi'

describe('App', () => {
  it('準備完了画面と正常な接続状態を表示する', async () => {
    const api: HealthApi = { checkHealth: async () => true }
    render(<App api={api} />)
    expect(screen.getByRole('heading', { name: 'Kizamu' })).toBeInTheDocument()
    expect(screen.getByText('プロジェクトの準備が完了しました。')).toBeInTheDocument()
    expect(await screen.findByText('サーバー接続: 正常')).toBeInTheDocument()
    expect(screen.queryByText(/count is/i)).not.toBeInTheDocument()
  })

  it('API失敗時に失敗を表示する', async () => {
    const api: HealthApi = { checkHealth: async () => { throw new Error('offline') } }
    render(<App api={api} />)
    expect(await screen.findByText('サーバー接続: 失敗')).toBeInTheDocument()
  })
})
