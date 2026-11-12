import { Project } from 'kizamu-domain-typescript'

type ProjectTabsState = Readonly<{
  projects: readonly Project[]
  selectedProject: Project | undefined
  projectName: string
  showProjectDialog: boolean
}>

type ProjectTabsAction =
  | { type: 'dialogOpened' }
  | { type: 'dialogCancelled' }
  | { type: 'projectNameChanged', name: string }
  | { type: 'projectRegistered', name: string }
  | { type: 'projectSelected', project: Project | undefined }

export const initialProjectTabsState: ProjectTabsState = {
  projects: [],
  selectedProject: undefined,
  projectName: '',
  showProjectDialog: false,
}

export function projectTabsReducer(state: ProjectTabsState, action: ProjectTabsAction): ProjectTabsState {
  switch (action.type) {
    case 'dialogOpened':
      return { ...state, showProjectDialog: true }
    case 'dialogCancelled':
      return { ...state, showProjectDialog: false }
    case 'projectNameChanged':
      return { ...state, projectName: action.name }
    case 'projectRegistered':
      return {
        ...state,
        projects: [...state.projects, new Project(action.name)],
        projectName: '',
        showProjectDialog: false,
      }
    case 'projectSelected':
      return { ...state, selectedProject: action.project }
  }
}
