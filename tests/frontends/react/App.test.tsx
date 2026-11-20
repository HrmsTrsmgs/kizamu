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
      const login = screen.getByRole('button', { name: 'Googleでログイン' })
      const loginPlace = login.parentElement

      fireEvent.click(login)

      ;(await screen.findByRole('button', { name: 'ログアウト' })).should.have.property('parentElement', loginPlace)
    })

    describe('ログインして記録を開始したとき', () => {
      beforeEach(async () => {
        fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))
        fireEvent.click(await screen.findByRole('button', { name: '記録を開始' }))
        await screen.findByRole('button', { name: '記録を終了' })
      })

      it('ログアウトすると作業記録一覧を隠してログインボタンを表示します', () => {
        fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

        screen.getByRole('button', { name: 'Googleでログイン' }).should.toBeInTheDocument()
        should.not.exist(screen.queryByRole('list', { name: '作業記録一覧' }))
      })

      it('再ログインすると前回の記録中状態を引き継ぎません', async () => {
        fireEvent.click(screen.getByRole('button', { name: 'ログアウト' }))

        fireEvent.click(screen.getByRole('button', { name: 'Googleでログイン' }))

        ;(await screen.findByRole('button', { name: '記録を開始' })).should.toBeInTheDocument()
        should.not.exist(screen.queryByText('記録中', { selector: '.status-badge' }))
      })
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

  it('初期表示ではプロジェクトの「未指定」タブが選択されています', () => {
    within(screen.getByRole('tablist', { name: 'プロジェクト' })).getByText('未指定').should.toHaveAttribute('aria-selected', 'true')
  })

  describe('プロジェクトAを登録したとき', () => {
    beforeEach(() => {
      プロジェクトを登録する('プロジェクトA')
    })

    it('登録済みプロジェクトのタブを押すとそのタブが選択中として表示されます', () => {
      const tabs = within(screen.getByRole('tablist', { name: 'プロジェクト' }))
      const project = tabs.getByRole('tab', { name: 'プロジェクトA' })

      fireEvent.click(project)

      project.should.toHaveAttribute('aria-selected', 'true')
      tabs.getByText('未指定').should.toHaveAttribute('aria-selected', 'false')
    })

    it('選択したプロジェクト名を記録画面の見出しに表示します', () => {
      fireEvent.click(screen.getByRole('tab', { name: 'プロジェクトA' }))

      screen.getByRole('heading', { name: 'プロジェクトA', level: 2 }).should.toBeInTheDocument()
    })

    it('登録済みプロジェクトの選択後に「未指定」タブを押すと選択表示と記録欄の見出しが未指定に戻ります', () => {
      const tabs = within(screen.getByRole('tablist', { name: 'プロジェクト' }))
      const project = tabs.getByRole('tab', { name: 'プロジェクトA' })
      tabs.getByText('未指定').should.toHaveAttribute('aria-selected', 'true')

      fireEvent.click(project)
      screen.getByRole('heading', { name: 'プロジェクトA', level: 2 }).should.toBeInTheDocument()

      const unspecified = tabs.getByRole('tab', { name: '未指定' })
      fireEvent.click(unspecified)

      unspecified.should.toHaveAttribute('aria-selected', 'true')
      project.should.toHaveAttribute('aria-selected', 'false')
      screen.getByRole('heading', { name: 'プロジェクト未指定', level: 2 }).should.toBeInTheDocument()
    })

    it('別のプロジェクトから記録中のタブに戻ると、記録の終了ボタンが表示されます', async () => {
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
