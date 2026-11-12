// @vitest-environment node
import { describe, it } from 'vitest'
import {
  getTimeRecorderControls,
  initialTimeRecorderOperationState,
  timeRecorderOperationReducer,
} from '../../../../src/frontends/react/src/presentation/timeRecorderOperationState'

describe('timeRecorderOperationReducer', () => {
  it('操作中は記録操作と再取得を停止し、成功すると再開します', () => {
    const busy = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'operationStarted')

    getTimeRecorderControls(busy).should.toStrictEqual({ canChangeRecord: false, canReload: false, notice: null })

    const completed = timeRecorderOperationReducer(busy, 'operationSucceeded')

    getTimeRecorderControls(completed).should.toStrictEqual({ canChangeRecord: true, canReload: true, notice: null })
  })

  it('操作に失敗しても最新状態が確認済みなら操作を続けられ、成功後も失敗の通知を保持します', () => {
    const failed = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'operationFailed')

    getTimeRecorderControls(failed).should.toStrictEqual({ canChangeRecord: true, canReload: true, notice: 'operationFailed' })

    const retrying = timeRecorderOperationReducer(failed, 'operationStarted')
    const completed = timeRecorderOperationReducer(retrying, 'operationSucceeded')

    getTimeRecorderControls(completed).should.have.property('notice', 'operationFailed')
  })

  it('未確認の案内を失敗通知より優先し、再取得に失敗しても再取得だけは再開できます', () => {
    const unconfirmed = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'recordsUnconfirmed')
    const failed = timeRecorderOperationReducer(unconfirmed, 'operationFailed')

    getTimeRecorderControls(failed).should.toStrictEqual({ canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' })

    const retrying = timeRecorderOperationReducer(failed, 'operationStarted')
    getTimeRecorderControls(retrying).should.toStrictEqual({ canChangeRecord: false, canReload: false, notice: 'stateUnconfirmed' })

    const retryFailed = timeRecorderOperationReducer(retrying, 'operationFailed')
    getTimeRecorderControls(retryFailed).should.toStrictEqual({ canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' })
  })

  it('再取得で一覧を確認すると操作を再開し、それ以前の失敗を通知します', () => {
    const unconfirmed = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'recordsUnconfirmed')
    const failed = timeRecorderOperationReducer(unconfirmed, 'operationFailed')
    const retrying = timeRecorderOperationReducer(failed, 'operationStarted')

    const confirmed = timeRecorderOperationReducer(retrying, 'recordsConfirmed')
    const completed = timeRecorderOperationReducer(confirmed, 'operationSucceeded')

    getTimeRecorderControls(completed).should.toStrictEqual({ canChangeRecord: true, canReload: true, notice: 'operationFailed' })
  })
})
