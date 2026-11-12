type TimeRecorderOperationState = Readonly<{
  isBusy: boolean
  isStateUnconfirmed: boolean
  hasFailed: boolean
}>

type OperationAction = 'operationStarted' | 'operationSucceeded' | 'operationFailed' | 'recordsConfirmed' | 'recordsUnconfirmed'

export type TimeRecorderControls = Readonly<{
  canChangeRecord: boolean
  canReload: boolean
  notice: 'stateUnconfirmed' | 'operationFailed' | null
}>

export const initialTimeRecorderOperationState: TimeRecorderOperationState = {
  isBusy: false,
  isStateUnconfirmed: false,
  hasFailed: false,
}

export function timeRecorderOperationReducer(state: TimeRecorderOperationState, action: OperationAction): TimeRecorderOperationState {
  switch (action) {
    case 'operationStarted':
      return { ...state, isBusy: true }
    case 'operationSucceeded':
      return { ...state, isBusy: false }
    case 'operationFailed':
      return { ...state, isBusy: false, hasFailed: true }
    case 'recordsConfirmed':
      return { ...state, isStateUnconfirmed: false }
    case 'recordsUnconfirmed':
      return { ...state, isStateUnconfirmed: true }
  }
}

export function getTimeRecorderControls(state: TimeRecorderOperationState): TimeRecorderControls {
  return {
    canChangeRecord: !state.isBusy && !state.isStateUnconfirmed,
    canReload: !state.isBusy,
    notice: state.isStateUnconfirmed ? 'stateUnconfirmed' : state.hasFailed ? 'operationFailed' : null,
  }
}
