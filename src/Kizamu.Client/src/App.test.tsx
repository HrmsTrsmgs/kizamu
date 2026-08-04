import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('準備完了画面を表示します', () => {
    const health = { checkHealth: async () => true }
    const timeRecords = {
      startTimeRecord: async () => ({ id: '', startedAt: '' }),
      endTimeRecord: async () => ({ id: '', startedAt: '', endedAt: '' }),
    }

    render(<App health={health} timeRecords={timeRecords} />)

    expect(screen.getByRole('heading', { name: 'Kizamu' })).toBeInTheDocument()
    expect(screen.getByText('プロジェクトの準備が完了しました。')).toBeInTheDocument()
    expect(screen.queryByText(/count is/i)).not.toBeInTheDocument()
  })
})
