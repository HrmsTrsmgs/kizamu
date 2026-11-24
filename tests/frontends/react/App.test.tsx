import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, it, vi, type Mock } from 'vitest'
import { should } from '../../should.mjs'
import App from '../../../src/frontends/react/src/App'
import type { HealthService } from '../../../src/frontends/react/src/presentation/healthService'
import type { TimeRecordService } from '../../../src/frontends/react/src/presentation/timeRecordService'
import { createTimeRecordServiceStub } from './test/createTimeRecordServiceStub'

let health: HealthService

beforeEach(() => {
  health = { checkHealth: async () => true }
})

describe('未ログインのApp', () => {
  describe('Googleログインを利用できるとき', () => {
    let createTimeRecords: Mock<() => TimeRecordService>

    beforeEach(() => {
      createTimeRecords = vi.fn(() => createTimeRecordServiceStub())
      render(<App health={health} signInWithGoogle={async () => 'google-id-token'} createTimeRecords={createTimeRecords} />)
    })

    it('Googleログインに成功するとログインボタンに代わって記録画面を表示します', async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

      ;(await screen.findByRole('button', { name: '記録を開始' })).should.toBeInTheDocument()
      should.not.exist(screen.queryByRole('button', { name: 'Googleでログイン' }))
    })

    it('Googleログインで取得したIDトークンを使って記録サービスを作成します', async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

      await waitFor(() => createTimeRecords.should.have.been.calledWith('google-id-token'))
    })

    it('Googleログインに成功すると、ログインボタンがあった場所にログアウトボタンが表示されます', async () => {
      const positionInMain = (button: HTMLElement) => {
        const children = screen.getByRole('main').children
        return Array.from(children).findIndex(child => child.contains(button))
      }
      const login = screen.getByRole('button', { name: 'Googleでログイン' })
      const loginPosition = positionInMain(login)
      loginPosition.should.be.at.least(0)

      fireEvent.click(login)

      const logout = await screen.findByRole('button', { name: 'ログアウト' })
      positionInMain(logout).should.equal(loginPosition)
    })

    it('ログイン後にログアウトすると作業記録一覧を隠してログインボタンを表示します', async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))
      await screen.findByRole('list', { name: '作業記録一覧' })

      fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

      screen.getByRole('button', { name: 'Googleでログイン' }).should.toBeInTheDocument()
      should.not.exist(screen.queryByRole('list', { name: '作業記録一覧' }))
    })

    it('記録中にログアウトして再ログインすると前回の記録中状態を引き継ぎません', async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))
      fireEvent.click(await screen.findByRole('button', { name: '記録を開始' }))
      await screen.findByRole('button', { name: '記録を終了' })

      fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))
      fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

      const startButton = await screen.findByRole('button', { name: '記録を開始' })
      startButton.should.toBeInTheDocument()
      should.not.exist(screen.queryByText('記録中', { selector: '.status-badge' }))
    })
  })

  it('ログイン前の画面にはGoogleのログインボタンが表示され、記録の開始ボタンは表示されません', () => {
    render(<App health={health} timeRecords={createTimeRecordServiceStub()} />)

    screen.getByRole('button', { name: 'Googleでログイン' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
  })
})

describe('ログイン済みのApp', () => {
  beforeEach(() => {
    render(<App isAuthenticated health={health} timeRecords={createTimeRecordServiceStub()} />)
  })

  it('初期認証済みの画面でログアウトすると作業記録一覧が消えてGoogleのログインボタンが表示されます', () => {
    screen.getByRole('list', { name: '作業記録一覧' }).should.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

    should.not.exist(screen.queryByRole('list', { name: '作業記録一覧' }))
    screen.getByRole('button', { name: 'Googleでログイン' }).should.toBeInTheDocument()
  })

  it('追加ボタンを押すとプロジェクト追加ダイアログを表示します', () => {
    fireEvent.click(screen.getByRole('button', { name: '追加' }))

    screen.getByRole('dialog', { name: 'プロジェクトを追加' }).should.toBeInTheDocument()
  })

  describe('プロジェクト追加ダイアログを開いたとき', () => {
    beforeEach(() => {
      fireEvent.click(screen.getByRole('button', { name: '追加' }))
    })

    it('登録直前に入力したプロジェクト名がタブに表示されます', () => {
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

    it('キャンセルボタンを押すとプロジェクト追加ダイアログを閉じます', () => {
      fireEvent.click(screen.getByRole('button', { name: 'キャンセル' }))

      should.not.exist(screen.queryByRole('dialog', { name: 'プロジェクトを追加' }))
    })
  })

  it('プロジェクトを登録すると追加ダイアログを閉じます', () => {
    プロジェクトを登録する('プロジェクトA')

    should.not.exist(screen.queryByRole('dialog', { name: 'プロジェクトを追加' }))
  })

  it('「未指定」と登録済みプロジェクトを切り替えると、タブの選択表示と記録欄の見出しが切り替わります', () => {
    const tabs = within(screen.getByRole('tablist', { name: 'プロジェクト' }))
    const unspecified = tabs.getByRole('tab', { name: '未指定' })
    unspecified.should.toHaveAttribute('aria-selected', 'true')

    プロジェクトを登録する('プロジェクトA')
    const project = tabs.getByRole('tab', { name: 'プロジェクトA' })
    unspecified.should.toHaveAttribute('aria-selected', 'true')

    fireEvent.click(project)

    project.should.toHaveAttribute('aria-selected', 'true')
    unspecified.should.toHaveAttribute('aria-selected', 'false')
    screen.getByRole('heading', { name: 'プロジェクトA', level: 2 }).should.toBeInTheDocument()

    fireEvent.click(unspecified)

    unspecified.should.toHaveAttribute('aria-selected', 'true')
    project.should.toHaveAttribute('aria-selected', 'false')
    screen.getByRole('heading', { name: 'プロジェクト未指定', level: 2 }).should.toBeInTheDocument()
  })

  it('別のプロジェクトから記録中のタブに戻ると、記録の終了ボタンが表示されます', async () => {
    const 記録中のプロジェクト = 'プロジェクトA'
    const 未開始のプロジェクト = 'プロジェクトB'
    await 記録中と未開始のタブを準備する(記録中のプロジェクト, 未開始のプロジェクト)

    fireEvent.click(screen.getByRole('tab', { name: 未開始のプロジェクト }))

    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: 記録中のプロジェクト }))
    screen.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
  })

  it('画面にアプリ名と用途を表示します', () => {
    screen.getByRole('heading', { name: 'Kizamu' }).should.toBeInTheDocument()
    screen.getByText('作業時間を記録します。').should.toBeInTheDocument()
  })

  it('デモ用のカウンターを表示しません', () => {
    should.not.exist(screen.queryByText(/count is/i))
  })
})

async function 記録中と未開始のタブを準備する(記録中のプロジェクト: string, 未開始のプロジェクト: string) {
  プロジェクトを登録する(記録中のプロジェクト)
  プロジェクトを登録する(未開始のプロジェクト)
  fireEvent.click(screen.getByRole('tab', { name: 記録中のプロジェクト }))
  fireEvent.click(screen.getByRole('button', { name: '記録を開始' }))
  await screen.findByRole('button', { name: '記録を終了' })
}

function プロジェクトを登録する(name: string) {
  fireEvent.click(screen.getByRole('button', { name: '追加' }))
  fireEvent.change(screen.getByRole('textbox', { name: 'プロジェクト名' }), { target: { value: name } })
  fireEvent.click(screen.getByRole('button', { name: '登録' }))
}
