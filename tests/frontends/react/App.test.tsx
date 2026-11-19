import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, it, vi } from 'vitest'
import { should } from '../../should.mjs'
import App from '../../../src/frontends/react/src/App'
import { createTimeRecordServiceStub } from './test/createTimeRecordServiceStub'

describe('未ログインのApp', () => {
  it('Googleログインに成功するとログインボタンに代わって記録画面を表示します', async () => {
    ログイン可能な画面を表示する()

    fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

    ;(await screen.findByRole('button', { name: '記録を開始' })).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: 'Googleでログイン' }))
  })

  it('Googleログインで取得したトークンを記録サービスの生成に渡します', async () => {
    const createTimeRecords = ログイン可能な画面を表示する()

    fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

    await waitFor(() => createTimeRecords.should.have.been.calledWith('google-id-token'))
  })

  it('ログアウトボタンをログインボタンと同じ場所に表示します', async () => {
    ログイン可能な画面を表示する()
    const login = screen.getByRole('button', { name: 'Googleでログイン' })
    const loginPlace = login.parentElement

    fireEvent.click(login)

    ;(await screen.findByRole('button', { name: 'ログアウト' })).should.have.property('parentElement', loginPlace)
  })

  it('ログアウトすると作業記録一覧を隠してログインボタンを表示します', async () => {
    await ログインして記録を開始する()

    fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

    screen.getByRole('button', { name: 'Googleでログイン' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('list', { name: '作業記録一覧' }))
  })

  it('再ログインすると前回の記録中状態を引き継ぎません', async () => {
    await ログインして記録を開始する()
    fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

    fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

    ;(await screen.findByRole('button', { name: '記録を開始' })).should.toBeInTheDocument()
    should.not.exist(screen.queryByText('記録中', { selector: '.status-badge' }))
  })

  it('Googleログインを表示し記録画面を表示しません', () => {
    render(<App health={{ checkHealth: async () => true }} timeRecords={createTimeRecordServiceStub()} />)

    screen.getByRole('button', { name: 'Googleでログイン' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
  })

  function ログイン可能な画面を表示する() {
    const createTimeRecords = vi.fn(() => createTimeRecordServiceStub())
    render(<App health={{ checkHealth: async () => true }} signInWithGoogle={async () => 'google-id-token'} createTimeRecords={createTimeRecords} />)
    return createTimeRecords
  }

  async function ログインして記録を開始する() {
    ログイン可能な画面を表示する()
    fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))
    fireEvent.click(await screen.findByRole('button', { name: '記録を開始' }))
    await screen.findByRole('button', { name: '記録を終了' })
  }
})

describe('App', () => {
  beforeEach(() => {
    render(<App isAuthenticated health={{ checkHealth: async () => true }} timeRecords={createTimeRecordServiceStub()} />)
  })

  it('追加ボタンを押すとプロジェクト追加ダイアログを表示します', () => {
    fireEvent.click(screen.getByRole('button', { name: '追加' }))

    screen.getByRole('dialog', { name: 'プロジェクトを追加' }).should.toBeInTheDocument()
  })

  it('登録直前に入力したプロジェクト名がタブに表示されます', () => {
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
  })

  it('プロジェクトを登録すると追加ダイアログを閉じます', () => {
    プロジェクトを登録する('プロジェクトA')

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
  })

  it('選択したプロジェクト名を記録画面の見出しに表示します', () => {
    プロジェクトを登録する('プロジェクトA')

    fireEvent.click(screen.getByRole('tab', { name: 'プロジェクトA' }))

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

  it('画面にアプリ名と用途を表示します', () => {
    screen.getByRole('heading', { name: 'Kizamu' }).should.toBeInTheDocument()
    screen.getByText('作業時間を記録します。').should.toBeInTheDocument()
  })

  it('デモ用のカウンターを表示しません', () => {
    should.not.exist(screen.queryByText(/count is/i))
  })
})

function プロジェクトを登録する(name: string) {
  fireEvent.click(screen.getByRole('button', { name: '追加' }))
  fireEvent.change(screen.getByRole('textbox', { name: 'プロジェクト名' }), { target: { value: name } })
  fireEvent.click(screen.getByRole('button', { name: '登録' }))
}
