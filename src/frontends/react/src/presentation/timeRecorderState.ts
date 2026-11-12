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
  | { type: 'recordStarted', record: TimeRecordPresentation, project?: Project }
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
      const updates = action.updatedRecords ?? []
      const records = mergeLoadedRecords(action.records, updates)
      return {
        ...state,
        records,
        timeRecordId: getEndTargetAfterLoad(state.timeRecordId, records, updates),
      }
    }
    case 'recordStarted':
      if ('project' in action && action.project !== state.selectedProject) {
        const projectTimeRecordIds = new Map(state.projectTimeRecordIds)
        projectTimeRecordIds.set(action.project, action.record.record.id)
        return { ...state, records: [...state.records, action.record], projectTimeRecordIds }
      }
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

function mergeLoadedRecords(
  loadedRecords: readonly TimeRecordPresentation[],
  updates: readonly TimeRecordPresentation[],
): TimeRecordPresentation[] {
  const latestUpdates = new Map<string, TimeRecordPresentation>()
  for (const update of updates) {
    const id = update.record.id
    // Records absent from the snapshot follow the order of their last update.
    latestUpdates.delete(id)
    latestUpdates.set(id, update)
  }

  const records = loadedRecords.map(loaded => latestUpdates.get(loaded.record.id) ?? loaded)
  for (const { record } of loadedRecords) latestUpdates.delete(record.id)
  return [...records, ...latestUpdates.values()]
}

function getEndTargetAfterLoad(
  previousId: string | null,
  records: readonly TimeRecordPresentation[],
  updates: readonly TimeRecordPresentation[],
): string | null {
  if (previousId !== null && updates.some(({ record }) => record.id === previousId && record.endedAt === null)) {
    return previousId
  }
  return records.find(({ record }) => record.endedAt === null)?.record.id ?? null
}
