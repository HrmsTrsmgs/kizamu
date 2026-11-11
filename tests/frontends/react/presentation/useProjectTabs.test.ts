import { act, renderHook } from '@testing-library/react'
import { Project } from 'kizamu-domain-typescript'
import { describe, it } from 'vitest'
import { useProjectTabs } from '../../../../src/frontends/react/src/presentation/useProjectTabs'

describe('useProjectTabs', () => {
  it('追加を開くとダイアログを表示します', () => {
    const { result } = renderHook(() => useProjectTabs())

    result.current.state.should.have.property('showProjectDialog', false)

    act(() => result.current.openProjectDialog())

    result.current.state.showProjectDialog.should.equal(true)
  })

  it('追加をキャンセルすると登録せずダイアログを閉じます', () => {
    const { result } = renderHook(() => useProjectTabs())

    act(() => result.current.openProjectDialog())
    act(() => result.current.cancelProjectDialog())

    result.current.state.showProjectDialog.should.equal(false)
    result.current.state.projects.should.toStrictEqual([])
  })

  it('追加画面から登録するとダイアログを閉じます', () => {
    const { result } = renderHook(() => useProjectTabs())

    act(() => result.current.openProjectDialog())
    act(() => result.current.registerProject('プロジェクトA'))

    result.current.state.showProjectDialog.should.equal(false)
  })

  it('登録したプロジェクトを一覧に保持します', () => {
    const { result } = renderHook(() => useProjectTabs())

    act(() => result.current.registerProject('プロジェクトA'))
    act(() => result.current.registerProject('プロジェクトB'))

    const projects = result.current.state.projects
    projects.should.toStrictEqual([new Project('プロジェクトA'), new Project('プロジェクトB')])
  })
})
