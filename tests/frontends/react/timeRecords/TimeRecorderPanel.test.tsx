import { render, screen, within } from '@testing-library/react'
import type { ComponentProps } from 'react'
import { TimeRecord } from 'kizamu-domain-typescript'
import { beforeEach, describe, it, vi } from 'vitest'
import { should } from '../../../should.mjs'
import TimeRecorderPanel from '../../../../src/frontends/react/src/timeRecords/TimeRecorderPanel'
import type { TimeRecorderControls } from '../../../../src/frontends/react/src/presentation/timeRecorderOperationState'
import { timestamp } from '../test/timeRecords'

const 開始時刻 = timestamp('2026-08-01T09:00:00Z').instant
const 終了時刻 = timestamp('2026-08-01T10:30:00Z').instant
const 終了済み記録 = TimeRecord.start('record', 開始時刻).end(終了時刻)
const 未確認時の操作: TimeRecorderControls = { canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' }

describe('TimeRecorderPanel', () => {
  let panel: ComponentProps<typeof TimeRecorderPanel>

  beforeEach(() => {
    panel = {
      controls: { canChangeRecord: true, canReload: true, notice: null },
      isRecording: false,
      toggleRecording: vi.fn(),
      reloadTimeRecords: vi.fn(),
    }
  })

  it('二つの操作欄はそれぞれのプロジェクトの見出しを参照します', () => {
    render(
      <>
        <TimeRecorderPanel {...panel} projectName="プロジェクトA" />
        <TimeRecorderPanel {...panel} projectName="プロジェクトB" />
      </>,
    )

    const regions = screen.getAllByRole('region')
    const headings = regions.map(region => within(region).getByRole('heading'))

    headings.map(heading => heading.textContent).should.deep.equal(['プロジェクトA', 'プロジェクトB'])
    new Set(headings.map(heading => heading.id)).size.should.equal(2)
    regions.map(region => region.getAttribute('aria-labelledby')).should.deep.equal(headings.map(heading => heading.id))
  })

  it('記録操作が許可されていない間は開始ボタンが無効になり、許可されると有効になります', () => {
    const { rerender } = render(<TimeRecorderPanel {...panel} controls={{ ...panel.controls, canChangeRecord: false }} />)
    screen.getByRole('button', { name: '記録を開始' }).should.toBeDisabled()

    rerender(<TimeRecorderPanel {...panel} />)

    screen.getByRole('button', { name: '記録を開始' }).should.toBeEnabled()
  })

  it('終了済みの記録を表示し、終了対象がなければ「待機中」と開始ボタンを表示します', () => {
    render(<TimeRecorderPanel {...panel} displayedRecord={終了済み記録} />)

    screen.getByRole('button', { name: '記録を開始' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を終了' }))
    screen.getByText('待機中').should.toBeInTheDocument()
  })

  it('記録中の操作欄には「記録中」と終了ボタンが表示され、開始ボタンは表示されません', () => {
    render(<TimeRecorderPanel {...panel} isRecording />)

    screen.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
    should.not.exist(screen.queryByRole('button', { name: '記録を開始' }))
    screen.getByText('記録中', { selector: '.status-badge' }).should.toBeInTheDocument()
  })

  it('操作欄の表示が終了済みの記録から新しい記録に変わると、前回の終了時刻が消えます', () => {
    const { rerender } = render(<TimeRecorderPanel {...panel} displayedRecord={終了済み記録} />)
    screen.getByLabelText('終了時刻').should.toBeInTheDocument()

    rerender(<TimeRecorderPanel {...panel} isRecording displayedRecord={TimeRecord.start('next', 終了時刻)} />)

    screen.getByRole('button', { name: '記録を終了' }).should.toBeInTheDocument()
    screen.getByLabelText('開始時刻').should.toHaveTextContent('2026/08/01 19:30')
    should.not.exist(screen.queryByLabelText('終了時刻'))
  })

  it.each([
    { isRecording: false, operationName: '記録を開始' },
    { isRecording: true, operationName: '記録を終了' },
  ])('記録一覧の最新状態が未確認の間は「$operationName」ボタンを押せません', ({ isRecording, operationName }) => {
    render(<TimeRecorderPanel {...panel} isRecording={isRecording} controls={未確認時の操作} />)

    screen.getByRole('button', { name: operationName }).should.toBeDisabled()
  })

  it('記録一覧の最新状態が未確認のときは、案内を表示し、再取得の通信中だけ再取得ボタンを無効にします', () => {
    const { rerender } = render(<TimeRecorderPanel {...panel} controls={未確認時の操作} />)
    screen.getByRole('alert').should.toHaveTextContent('最新状態を確認できません')
    screen.getByRole('button', { name: '再取得' }).should.toBeEnabled()

    rerender(<TimeRecorderPanel {...panel} controls={{ ...未確認時の操作, canReload: false }} />)

    screen.getByRole('button', { name: '再取得' }).should.toBeDisabled()
  })
})
