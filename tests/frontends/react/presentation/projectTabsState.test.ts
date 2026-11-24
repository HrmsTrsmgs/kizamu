// @vitest-environment node
import { Project } from 'kizamu-domain-typescript'
import { beforeEach, describe, it } from 'vitest'
import { initialProjectTabsState, projectTabsReducer } from '../../../../src/frontends/react/src/presentation/projectTabsState'

describe('projectTabsReducer', () => {
  it('入力したプロジェクト名を保持します', () => {
    initialProjectTabsState.projectName.should.equal('')

    const state = projectTabsReducer(initialProjectTabsState, { type: 'projectNameChanged', name: 'プロジェクトA' })

    state.projectName.should.equal('プロジェクトA')
  })

  it('プロジェクトの追加を始めると、追加ダイアログが開きます', () => {
    initialProjectTabsState.showProjectDialog.should.equal(false)

    const state = projectTabsReducer(initialProjectTabsState, { type: 'dialogOpened' })

    state.showProjectDialog.should.equal(true)
  })

  describe('追加ダイアログを開いたとき', () => {
    let opened: typeof initialProjectTabsState

    beforeEach(() => {
      opened = projectTabsReducer(initialProjectTabsState, { type: 'dialogOpened' })
    })

    it('追加をキャンセルするとダイアログを閉じます', () => {
      const state = projectTabsReducer(opened, { type: 'dialogCancelled' })

      state.showProjectDialog.should.equal(false)
    })

    it('プロジェクトの追加をキャンセルすると、登録済みの一覧は空のままです', () => {
      const state = projectTabsReducer(opened, { type: 'dialogCancelled' })

      state.projects.should.toStrictEqual([])
    })

    it('プロジェクトを登録して追加ダイアログを開き直すと、名前の入力欄は空です', () => {
      const named = projectTabsReducer(opened, { type: 'projectNameChanged', name: 'プロジェクトA' })

      const registered = projectTabsReducer(named, { type: 'projectRegistered' })
      const reopened = projectTabsReducer(registered, { type: 'dialogOpened' })

      reopened.projectName.should.equal('')
    })
  })

  it('入力したプロジェクトを登録すると追加ダイアログが閉じます', () => {
    const opened = { ...initialProjectTabsState, showProjectDialog: true, projectName: 'プロジェクトA' }

    const state = projectTabsReducer(opened, { type: 'projectRegistered' })

    state.showProjectDialog.should.equal(false)
  })

  describe('プロジェクトAを登録したとき', () => {
    let registered: typeof initialProjectTabsState

    beforeEach(() => {
      const named = { ...initialProjectTabsState, projectName: 'プロジェクトA' }
      registered = projectTabsReducer(named, { type: 'projectRegistered' })
    })

    it('二つのプロジェクトを順に登録すると、一覧に両方が登録順に並びます', () => {
      const next = projectTabsReducer(registered, { type: 'projectNameChanged', name: 'プロジェクトB' })

      const state = projectTabsReducer(next, { type: 'projectRegistered' })

      state.projects.should.toStrictEqual([new Project('プロジェクトA'), new Project('プロジェクトB')])
    })
  })
})
