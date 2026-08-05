import type { TimeRecordResponse } from './timeRecordApi'

export type TimeRecorderState = {
  records: TimeRecordResponse[]
  timeRecordId: string | null
  startedAt: string | null
  endedAt: string | null
}

export type TimeRecorderAction =
  | { type: 'recordsLoaded', records: TimeRecordResponse[] }
  | { type: 'recordStarted', record: TimeRecordResponse }
  | { type: 'recordEnded', record: TimeRecordResponse }

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
        records: state.records.map(record =>
          record.id === action.record.id ? action.record : record),
        timeRecordId: null,
        endedAt: action.record.endedAt,
      }
  }
}
