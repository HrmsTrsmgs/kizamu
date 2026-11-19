// @vitest-environment node
import { beforeEach, describe, it } from 'vitest'
import {
  getTimeRecorderControls,
  initialTimeRecorderOperationState,
  timeRecorderOperationReducer,
} from '../../../../src/frontends/react/src/presentation/timeRecorderOperationState'

describe('timeRecorderOperationReducer', () => {
  it('記録操作の処理中は記録の変更と一覧の再取得ができず、成功後は両方を実行できます', () => {
    const busy = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'operationStarted')

    getTimeRecorderControls(busy).should.toStrictEqual({ canChangeRecord: false, canReload: false, notice: null })

    const completed = timeRecorderOperationReducer(busy, 'operationSucceeded')

    getTimeRecorderControls(completed).should.toStrictEqual({ canChangeRecord: true, canReload: true, notice: null })
  })

  it('一覧の最新状態を確認できていれば、記録操作に失敗しても記録の変更と再取得ができ、失敗通知が残ります', () => {
    const failed = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'operationFailed')

    getTimeRecorderControls(failed).should.toStrictEqual({ canChangeRecord: true, canReload: true, notice: 'operationFailed' })
  })

  it('失敗後に次の操作が成功しても以前の失敗通知を保持します', () => {
    const failed = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'operationFailed')
    const retrying = timeRecorderOperationReducer(failed, 'operationStarted')
    const completed = timeRecorderOperationReducer(retrying, 'operationSucceeded')

    getTimeRecorderControls(completed).should.have.property('notice', 'operationFailed')
  })

  describe('一覧が未確認のとき', () => {
    let unconfirmed: typeof initialTimeRecorderOperationState

    beforeEach(() => {
      unconfirmed = timeRecorderOperationReducer(initialTimeRecorderOperationState, 'recordsUnconfirmed')
    })

    it('記録操作に失敗して一覧も未確認なら、失敗通知より未確認の案内を優先し、再取得だけを許可します', () => {
      const failed = timeRecorderOperationReducer(unconfirmed, 'operationFailed')

      getTimeRecorderControls(failed).should.toStrictEqual({ canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' })
    })

    describe('操作に失敗して再取得するとき', () => {
      let failed: typeof initialTimeRecorderOperationState

      beforeEach(() => {
        failed = timeRecorderOperationReducer(unconfirmed, 'operationFailed')
      })

      it('未確認の一覧を再取得している間はすべての操作を止め、失敗後は再取得だけを再開します', () => {
        const retrying = timeRecorderOperationReducer(failed, 'operationStarted')
        getTimeRecorderControls(retrying).should.toStrictEqual({ canChangeRecord: false, canReload: false, notice: 'stateUnconfirmed' })

        const retryFailed = timeRecorderOperationReducer(retrying, 'operationFailed')
        getTimeRecorderControls(retryFailed).should.toStrictEqual({ canChangeRecord: false, canReload: true, notice: 'stateUnconfirmed' })
      })

      it('再取得で一覧を確認すると操作を再開し、それ以前の失敗を通知します', () => {
        const retrying = timeRecorderOperationReducer(failed, 'operationStarted')

        const confirmed = timeRecorderOperationReducer(retrying, 'recordsConfirmed')
        const completed = timeRecorderOperationReducer(confirmed, 'operationSucceeded')

        getTimeRecorderControls(completed).should.toStrictEqual({ canChangeRecord: true, canReload: true, notice: 'operationFailed' })
      })
    })
  })
})
