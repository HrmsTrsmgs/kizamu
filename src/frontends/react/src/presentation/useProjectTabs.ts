import { useReducer } from 'react'
import { initialProjectTabsState, projectTabsReducer } from './projectTabsState'

export function useProjectTabs() {
  const [state, dispatch] = useReducer(projectTabsReducer, initialProjectTabsState)

  return {
    projects: state.projects,
    registration: {
      isOpen: state.showProjectDialog,
      projectName: state.projectName,
      open: () => dispatch({ type: 'dialogOpened' }),
      changeName: (name: string) => dispatch({ type: 'projectNameChanged', name }),
      cancel: () => dispatch({ type: 'dialogCancelled' }),
      register: () => dispatch({ type: 'projectRegistered' }),
    },
    reset: () => dispatch({ type: 'signedOut' }),
  }
}
