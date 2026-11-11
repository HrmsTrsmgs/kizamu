import { fireEvent, render, screen } from '@testing-library/react'
import { describe, it } from 'vitest'
import { should } from '../../should.mjs'
import App from '../../../src/frontends/react/src/App'
import { createTimeRecordServiceStub } from './test/createTimeRecordServiceStub'

describe('App', () => {
  it('追加ボタンを押すとプロジェクト追加ダイアログを表示します', () => {
    const health = { checkHealth: async () => true }
    const timeRecords = createTimeRecordServiceStub()

    render(<App health={health} timeRecords={timeRecords} />)

    fireEvent.click(screen.getByRole('button', { name: '追加' }))

    screen.getByRole('dialog', { name: 'プロジェクトを追加' }).should.toBeInTheDocument()
  })

  it('キャンセルボタンを押すとプロジェクト追加ダイアログを閉じます', () => {
    const health = { checkHealth: async () => true }
    const timeRecords = createTimeRecordServiceStub()

    render(<App health={health} timeRecords={timeRecords} />)
    fireEvent.click(screen.getByRole('button', { name: '追加' }))

    fireEvent.click(screen.getByRole('button', { name: 'キャンセル' }))

    should.not.exist(screen.queryByRole('dialog', { name: 'プロジェクトを追加' }))
  })

  it('準備完了画面を表示します', () => {
    const health = { checkHealth: async () => true }
    const timeRecords = createTimeRecordServiceStub()

    render(<App health={health} timeRecords={timeRecords} />)

    screen.getByRole('heading', { name: 'Kizamu' }).should.toBeInTheDocument()
    screen.getByText('作業時間を記録します。').should.toBeInTheDocument()
    should.not.exist(screen.queryByText(/count is/i))
  })
})
