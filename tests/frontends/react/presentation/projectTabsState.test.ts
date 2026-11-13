// @vitest-environment node
import { Project } from 'kizamu-domain-typescript'
import { describe, it } from 'vitest'
import { initialProjectTabsState, projectTabsReducer } from '../../../../src/frontends/react/src/presentation/projectTabsState'

describe('projectTabsReducer', () => {
  it('入力したプロジェクト名を保持します', () => {
    initialProjectTabsState.projectName.should.equal('')

    const state = projectTabsReducer(initialProjectTabsState, { type: 'projectNameChanged', name: 'プロジェクトA' })

    state.projectName.should.equal('プロジェクトA')
  })

  it('追加を開くとダイアログを表示します', () => {
    initialProjectTabsState.showProjectDialog.should.equal(false)

    const state = projectTabsReducer(initialProjectTabsState, { type: 'dialogOpened' })

    state.showProjectDialog.should.equal(true)
  })

  it('追加をキャンセルすると登録せずダイアログを閉じます', () => {
    const opened = projectTabsReducer(initialProjectTabsState, { type: 'dialogOpened' })

    const state = projectTabsReducer(opened, { type: 'dialogCancelled' })

    state.showProjectDialog.should.equal(false)
    state.projects.should.toStrictEqual([])
  })

  it('追加画面から登録するとダイアログを閉じます', () => {
    const opened = { ...initialProjectTabsState, showProjectDialog: true, projectName: 'プロジェクトA' }

    const state = projectTabsReducer(opened, { type: 'projectRegistered' })

    state.showProjectDialog.should.equal(false)
  })

  it('登録後に追加を開くとプロジェクト名は空です', () => {
    const opened = projectTabsReducer(initialProjectTabsState, { type: 'dialogOpened' })
    const named = projectTabsReducer(opened, { type: 'projectNameChanged', name: 'プロジェクトA' })

    const registered = projectTabsReducer(named, { type: 'projectRegistered' })
    const reopened = projectTabsReducer(registered, { type: 'dialogOpened' })

    reopened.projectName.should.equal('')
  })

  it('登録したプロジェクトを選択すると選択した対象を保持します', () => {
    const named = { ...initialProjectTabsState, projectName: 'プロジェクトA' }
    const registered = projectTabsReducer(named, { type: 'projectRegistered' })
    const project = registered.projects[0]

    const state = projectTabsReducer(registered, { type: 'projectSelected', project })

    state.should.have.property('selectedProject', project)
  })

  it('登録したプロジェクトを一覧に保持します', () => {
    const named = { ...initialProjectTabsState, projectName: 'プロジェクトA' }
    const first = projectTabsReducer(named, { type: 'projectRegistered' })
    const next = projectTabsReducer(first, { type: 'projectNameChanged', name: 'プロジェクトB' })

    const state = projectTabsReducer(next, { type: 'projectRegistered' })

    state.projects.should.toStrictEqual([new Project('プロジェクトA'), new Project('プロジェクトB')])
  })
})
