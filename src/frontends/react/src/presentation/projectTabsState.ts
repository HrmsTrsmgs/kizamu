import { Project } from 'kizamu-domain-typescript'

type ProjectTabsState = Readonly<{
  projects: readonly Project[]
  projectName: string
  showProjectDialog: boolean
}>

type ProjectTabsAction =
  | { type: 'signedOut' }
  | { type: 'dialogOpened' }
  | { type: 'dialogCancelled' }
  | { type: 'projectNameChanged', name: string }
  | { type: 'projectRegistered' }

export const initialProjectTabsState: ProjectTabsState = {
  projects: [],
  projectName: '',
  showProjectDialog: false,
}

export function projectTabsReducer(state: ProjectTabsState, action: ProjectTabsAction): ProjectTabsState {
  switch (action.type) {
    case 'signedOut':
      return initialProjectTabsState
    case 'dialogOpened':
      return { ...state, showProjectDialog: true }
    case 'dialogCancelled':
      return { ...state, showProjectDialog: false }
    case 'projectNameChanged':
      return { ...state, projectName: action.name }
    case 'projectRegistered':
      return {
        ...state,
        projects: [...state.projects, new Project(state.projectName)],
        projectName: '',
        showProjectDialog: false,
      }
  }
}
