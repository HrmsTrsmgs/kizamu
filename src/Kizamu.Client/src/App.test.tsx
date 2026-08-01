import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import type { HealthApi } from './healthApi'

describe('App', () => {
  it('準備完了画面と正常な接続状態を表示する', async () => {
    const health: HealthApi = { checkHealth: async () => true }
    const timeRecords = { startTimeRecord: async () => ({ id: '', startedAt: '' }), endTimeRecord: async () => undefined }
    render(<App health={health} timeRecords={timeRecords} />)
    expect(screen.getByRole('heading', { name: 'Kizamu' })).toBeInTheDocument()
    expect(screen.getByText('プロジェクトの準備が完了しました。')).toBeInTheDocument()
    expect(await screen.findByText('サーバー接続: 正常')).toBeInTheDocument()
    expect(screen.queryByText(/count is/i)).not.toBeInTheDocument()
  })

  it('API失敗時に失敗を表示する', async () => {
    const health: HealthApi = { checkHealth: async () => { throw new Error('offline') } }
    const timeRecords = { startTimeRecord: async () => ({ id: '', startedAt: '' }), endTimeRecord: async () => undefined }
    render(<App health={health} timeRecords={timeRecords} />)
    expect(await screen.findByText('サーバー接続: 失敗')).toBeInTheDocument()
  })

  it('開始ボタンを押すと記録開始を要求します', () => {
    const startTimeRecord = vi.fn(async () => ({
      id: '019cde5b-4c00-7000-8000-000000000001',
      startedAt: '2026-08-01T09:00:00Z',
    }))
    const health = { checkHealth: async () => true }
    const timeRecords = { startTimeRecord, endTimeRecord: async () => undefined }
    render(<App health={health} timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    expect(startTimeRecord).toHaveBeenCalledOnce()
  })

  it('記録を開始すると開始時刻が表示されます', async () => {
    const startedAt = '2026-08-01T09:00:00Z'
    const health = { checkHealth: async () => true }
    const timeRecords = {
      startTimeRecord: async () => ({
        id: '019cde5b-4c00-7000-8000-000000000001',
        startedAt,
      }),
      endTimeRecord: async () => undefined,
    }
    render(<App health={health} timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    const startedAtDisplay = await screen.findByLabelText('開始時刻')
    expect(startedAtDisplay).toHaveAttribute('datetime', startedAt)
    expect(startedAtDisplay).not.toBeEmptyDOMElement()
  })

  it('記録を開始すると終了操作へ切り替わります', async () => {
    const health = { checkHealth: async () => true }
    const timeRecords = {
      startTimeRecord: async () => ({
        id: '019cde5b-4c00-7000-8000-000000000001',
        startedAt: '2026-08-01T09:00:00Z',
      }),
      endTimeRecord: async () => undefined,
    }
    render(<App health={health} timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))

    expect(await screen.findByRole('button', { name: '記録を終了' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '記録を開始' })).not.toBeInTheDocument()
  })

  it('終了ボタンを押すと開始した記録の終了を要求します', async () => {
    const id = '019cde5b-4c00-7000-8000-000000000001'
    const health = { checkHealth: async () => true }
    const endTimeRecord = vi.fn(async () => undefined)
    const timeRecords = {
      startTimeRecord: async () => ({
        id,
        startedAt: '2026-08-01T09:00:00Z',
      }),
      endTimeRecord,
    }
    render(<App health={health} timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を終了' }))

    expect(endTimeRecord).toHaveBeenCalledOnce()
    expect(endTimeRecord).toHaveBeenCalledWith(id)
  })
})
