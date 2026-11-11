import type { Project } from 'kizamu-domain-typescript'
import { getTimeRecordTimestamps, type RecordTimestamp, type TimeRecordPresentation } from './timeRecordPresentation'

export type TimeRecorderState = Readonly<{
  selectedProject?: Project
  projectTimeRecordIds?: ReadonlyMap<Project | undefined, string | null>
  records: readonly TimeRecordPresentation[]
  timeRecordId: string | null
  startedAt: RecordTimestamp | null
  endedAt: RecordTimestamp | null
}>

export type TimeRecorderAction =
  | { type: 'projectSelected', project: Project | undefined }
  | { type: 'recordsLoaded', records: readonly TimeRecordPresentation[], updatedRecords?: readonly TimeRecordPresentation[] }
  | { type: 'recordStarted', record: TimeRecordPresentation }
  | { type: 'recordEnded', record: TimeRecordPresentation }

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
    case 'projectSelected': {
      const projectTimeRecordIds = new Map(state.projectTimeRecordIds)
      projectTimeRecordIds.set(state.selectedProject, state.timeRecordId)
      return {
        ...state,
        selectedProject: action.project,
        projectTimeRecordIds,
        timeRecordId: projectTimeRecordIds.get(action.project) ?? null,
      }
    }
    case 'recordsLoaded': {
      const updatedRecords = action.updatedRecords ?? []
      const latestUpdates = [...updatedRecords].reverse()
      const records = [
        ...action.records.map(loaded => latestUpdates.find(({ record }) => record.id === loaded.record.id) ?? loaded),
        ...updatedRecords.filter(({ record }, index) =>
          !action.records.some(loaded => loaded.record.id === record.id)
          && !updatedRecords.slice(index + 1).some(updated => updated.record.id === record.id)),
      ]
      return {
        ...state,
        records,
        timeRecordId:
          latestUpdates.find(({ record }) => record.id === state.timeRecordId && record.endedAt === null)?.record.id
          ?? records.find(({ record }) => record.endedAt === null)?.record.id ?? null,
      }
    }
    case 'recordStarted':
      return {
        ...state,
        records: [...state.records, action.record],
        timeRecordId: action.record.record.id,
        startedAt: getTimeRecordTimestamps(action.record).startedAt,
        endedAt: null,
      }
    case 'recordEnded':
      return {
        ...state,
        records: state.records.map(record =>
          record.record.id === action.record.record.id ? action.record : record),
        timeRecordId: null,
        endedAt: getTimeRecordTimestamps(action.record).endedAt,
      }
  }
}
