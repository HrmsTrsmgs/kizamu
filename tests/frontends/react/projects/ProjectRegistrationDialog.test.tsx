import { render, screen, within } from '@testing-library/react'
import { beforeEach, describe, it, vi } from 'vitest'
import ProjectRegistrationDialog from '../../../../src/frontends/react/src/projects/ProjectRegistrationDialog'

describe('ProjectRegistrationDialogを二つ表示したとき', () => {
  beforeEach(() => {
    const props = { projectName: '', onProjectNameChange: vi.fn(), onRegister: vi.fn(), onCancel: vi.fn() }

    render(
      <>
        <ProjectRegistrationDialog {...props} />
        <ProjectRegistrationDialog {...props} />
      </>,
    )
  })

  it('二つの登録ダイアログはそれぞれの見出しを参照します', () => {
    const dialogs = screen.getAllByRole('dialog', { name: 'プロジェクトを追加' })
    const headingIds = dialogs.map(dialog => within(dialog).getByRole('heading', { name: 'プロジェクトを追加' }).id)

    dialogs.should.have.length(2)
    new Set(headingIds).size.should.equal(2)
    dialogs.map(dialog => dialog.getAttribute('aria-labelledby')).should.deep.equal(headingIds)
  })

  it('二つの登録ダイアログのラベルはそれぞれの名前入力欄を参照します', () => {
    const dialogs = screen.getAllByRole('dialog')
    const inputIds = dialogs.map(dialog => within(dialog).getByRole('textbox', { name: 'プロジェクト名' }).id)

    dialogs.should.have.length(2)
    new Set(inputIds).size.should.equal(2)
    dialogs.map(dialog => within(dialog).getByText('プロジェクト名').getAttribute('for')).should.deep.equal(inputIds)
  })
})
