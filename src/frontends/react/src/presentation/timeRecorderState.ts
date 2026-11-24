import type { Project } from 'kizamu-domain-typescript'
import type { TimeRecordPresentation } from './timeRecordPresentation'

export type TimeRecorderState = Readonly<{
  selectedProject?: Project
  projectTimeRecordIds?: ReadonlyMap<Project | undefined, string | null>
  records: readonly TimeRecordPresentation[]
  timeRecordId: string | null
  displayedRecord: TimeRecordPresentation | null
}>

export type TimeRecorderAction =
  | { type: 'sessionEnded' }
  | { type: 'projectSelected', project: Project | undefined }
  | { type: 'recordsLoaded', records: readonly TimeRecordPresentation[], updatedRecords?: readonly TimeRecordPresentation[] }
  | { type: 'recordStarted', record: TimeRecordPresentation, project: Project | undefined }
  | { type: 'recordEnded', record: TimeRecordPresentation, project: Project | undefined }

export const initialTimeRecorderState: TimeRecorderState = {
  records: [],
  timeRecordId: null,
  displayedRecord: null,
}

export function timeRecorderReducer(
  state: TimeRecorderState,
  action: TimeRecorderAction,
): TimeRecorderState {
  switch (action.type) {
    case 'sessionEnded':
      return { ...initialTimeRecorderState, selectedProject: state.selectedProject }
    case 'projectSelected': {
      const projectTimeRecordIds = new Map(state.projectTimeRecordIds).set(state.selectedProject, state.timeRecordId)
      return {
        ...state,
        selectedProject: action.project,
        projectTimeRecordIds,
        timeRecordId: projectTimeRecordIds.get(action.project) ?? null,
      }
    }
    case 'recordsLoaded':
      return applyLoadedRecords(state, action.records, action.updatedRecords ?? [])
    case 'recordStarted':
      return {
        ...state,
        ...updateEndTarget(state, action.project, action.record.record.id),
        records: [...state.records, action.record],
        displayedRecord: action.project === state.selectedProject ? action.record : state.displayedRecord,
      }
    case 'recordEnded':
      return {
        ...state,
        ...updateEndTarget(state, action.project, null),
        records: state.records.map(record => record.record.id === action.record.record.id ? action.record : record),
        displayedRecord: state.displayedRecord === null ? null : action.record,
      }
  }
}

// 操作の応答は、完了時に表示されているタブにかかわらず、要求元の終了対象を更新します。
function updateEndTarget(state: TimeRecorderState, project: Project | undefined, id: string | null) {
  return project === state.selectedProject
    ? { timeRecordId: id }
    : { projectTimeRecordIds: new Map(state.projectTimeRecordIds).set(project, id) }
}

function applyLoadedRecords(
  state: TimeRecorderState,
  loadedRecords: readonly TimeRecordPresentation[],
  updates: readonly TimeRecordPresentation[],
): TimeRecorderState {
  const records = mergeLoadedRecords(loadedRecords, updates)
  const displayedRecord = records.find(({ record }) => record.id === state.displayedRecord?.record.id) ?? state.displayedRecord
  const timeRecordId = getEndTargetAfterLoad(state, records, updates)
  const nextState = { ...state, records, timeRecordId, displayedRecord }
  if (!state.projectTimeRecordIds) return nextState

  const projectTimeRecordIds = new Map(state.projectTimeRecordIds)
  for (const [project, id] of projectTimeRecordIds) {
    if (id !== null && !records.some(({ record }) => record.id === id && record.endedAt === null)) {
      projectTimeRecordIds.set(project, null)
    }
  }
  return { ...nextState, projectTimeRecordIds }
}

function mergeLoadedRecords(
  loadedRecords: readonly TimeRecordPresentation[],
  updates: readonly TimeRecordPresentation[],
): TimeRecordPresentation[] {
  const latestUpdates = new Map<string, TimeRecordPresentation>()
  for (const update of updates) {
    const id = update.record.id
    // Records absent from the snapshot follow the order of their first update.
    latestUpdates.set(id, update)
  }

  const records = loadedRecords.map(loaded => latestUpdates.get(loaded.record.id) ?? loaded)
  for (const { record } of loadedRecords) latestUpdates.delete(record.id)
  return [...records, ...latestUpdates.values()]
}

function getEndTargetAfterLoad(
  state: TimeRecorderState,
  records: readonly TimeRecordPresentation[],
  updates: readonly TimeRecordPresentation[],
): string | null {
  const previousId = state.timeRecordId
  if (previousId !== null && updates.some(({ record }) => record.id === previousId && record.endedAt === null)) {
    return previousId
  }
  const otherProjectIds = Array.from(state.projectTimeRecordIds ?? [])
    .filter(([project]) => project !== state.selectedProject)
    .map(([, id]) => id)
  return records.find(({ record }) => record.endedAt === null && !otherProjectIds.includes(record.id))?.record.id ?? null
}
