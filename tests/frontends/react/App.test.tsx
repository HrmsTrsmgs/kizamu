import { act, fireEvent, render, screen, within } from '@testing-library/react'
import { beforeEach, describe, it } from 'vitest'
import { should } from '../../should.mjs'
import App from '../../../src/frontends/react/src/App'
import { createTimeRecordServiceStub } from './test/createTimeRecordServiceStub'

describe('App', () => {
  beforeEach(() => {
    render(<App health={{ checkHealth: async () => true }} timeRecords={createTimeRecordServiceStub()} />)
  })

  it('追加ボタンを押すとプロジェクト追加ダイアログを表示します', () => {
    fireEvent.click(screen.getByRole('button', { name: '追加' }))

    screen.getByRole('dialog', { name: 'プロジェクトを追加' }).should.toBeInTheDocument()
  })

  it('入力したプロジェクト名がタブに表示されます', () => {
    fireEvent.click(screen.getByRole('button', { name: '追加' }))
    const dialog = screen.getByRole('dialog', { name: 'プロジェクトを追加' })
    const projectName = within(dialog).getByRole('textbox', { name: 'プロジェクト名' })
    fireEvent.change(projectName, { target: { value: '入力途中' } })

    act(() => {
      fireEvent.change(projectName, { target: { value: 'プロジェクトA' } })
      projectName.should.toHaveValue('プロジェクトA')
      fireEvent.click(within(dialog).getByRole('button', { name: '登録' }))
    })

    screen.getByRole('tab', { name: 'プロジェクトA' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('dialog', { name: 'プロジェクトを追加' }))
  })

  it('初期状態では未指定を選択中として表示します', () => {
    within(screen.getByRole('tablist', { name: 'プロジェクト' })).getByText('未指定').should.toHaveAttribute('aria-selected', 'true')
  })

  it('登録済みプロジェクトのタブを押すとそのタブが選択中として表示されます', () => {
    プロジェクトを登録する('プロジェクトA')
    const tabs = within(screen.getByRole('tablist', { name: 'プロジェクト' }))
    const project = tabs.getByRole('tab', { name: 'プロジェクトA' })

    fireEvent.click(project)

    project.should.toHaveAttribute('aria-selected', 'true')
    tabs.getByText('未指定').should.toHaveAttribute('aria-selected', 'false')
    screen.getByRole('heading', { name: 'プロジェクトA', level: 2 }).should.toBeInTheDocument()
  })

  it('タブを切り替えて戻ると記録中の対象には終了操作を表示します', async () => {
    プロジェクトを登録する('プロジェクトA')
    プロジェクトを登録する('プロジェクトB')
    fireEvent.click(screen.getByRole('tab', { name: 'プロジェクトA' }))
    fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
    await screen.findByRole('button', { name: '記録を終了' })

    fireEvent.click(screen.getByRole('tab', { name: 'プロジェクトB' }))

    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: 'プロジェクトA' }))
    screen.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
  })

  it('キャンセルボタンを押すとプロジェクト追加ダイアログを閉じます', () => {
    fireEvent.click(screen.getByRole('button', { name: '追加' }))

    fireEvent.click(screen.getByRole('button', { name: 'キャンセル' }))

    should.not.exist(screen.queryByRole('dialog', { name: 'プロジェクトを追加' }))
  })

  it('準備完了画面を表示します', () => {
    screen.getByRole('heading', { name: 'Kizamu' }).should.toBeInTheDocument()
    screen.getByText('作業時間を記録します。').should.toBeInTheDocument()
    should.not.exist(screen.queryByText(/count is/i))
  })
})

function プロジェクトを登録する(name: string) {
  fireEvent.click(screen.getByRole('button', { name: '追加' }))
  fireEvent.change(screen.getByRole('textbox', { name: 'プロジェクト名' }), { target: { value: name } })
  fireEvent.click(screen.getByRole('button', { name: '登録' }))
}
