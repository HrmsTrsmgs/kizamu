import { act, renderHook } from '@testing-library/react'
import { Project } from 'kizamu-domain-typescript'
import { describe, it } from 'vitest'
import { useProjectTabs } from '../../../../src/frontends/react/src/presentation/useProjectTabs'

describe('useProjectTabs', () => {
  it('登録したプロジェクトを一覧に保持します', () => {
    const { result } = renderHook(() => useProjectTabs())

    act(() => result.current.registerProject('プロジェクトA'))
    act(() => result.current.registerProject('プロジェクトB'))

    const projects = result.current.state.projects
    projects.should.toStrictEqual([new Project('プロジェクトA'), new Project('プロジェクトB')])
  })
})
