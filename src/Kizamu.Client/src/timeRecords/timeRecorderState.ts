import type { EndTimeRecordResponse, StartTimeRecordResponse } from './timeRecordApi'

export type TimeRecorderState = {
  records: StartTimeRecordResponse[]
  timeRecordId: string | null
  startedAt: string | null
  endedAt: string | null
}

export type TimeRecorderAction =
  | { type: 'recordsLoaded', records: StartTimeRecordResponse[] }
  | { type: 'recordStarted', record: StartTimeRecordResponse }
  | { type: 'recordEnded', record: EndTimeRecordResponse }

export const initialTimeRecorderState: TimeRecorderState = {
  records: [],
  timeRecordId: null,
  startedAt: null,
  endedAt: null,
}

export function timeRecorderReducer(
  state: TimeRecorderState,
  action: TimeRecorderAction,
): TimeRecorderState {
  switch (action.type) {
    case 'recordsLoaded':
      return { ...state, records: action.records }
    case 'recordStarted':
      return {
        ...state,
        records: [...state.records, action.record],
        timeRecordId: action.record.id,
        startedAt: action.record.startedAt,
        endedAt: null,
      }
    case 'recordEnded':
      return {
        ...state,
        timeRecordId: null,
        endedAt: action.record.endedAt,
      }
  }
}
