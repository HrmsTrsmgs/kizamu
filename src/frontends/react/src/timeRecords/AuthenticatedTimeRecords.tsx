import { useReducer } from 'react'
import { initialProjectTabsState, projectTabsReducer } from '../presentation/projectTabsState'
import type { TimeRecordService } from '../presentation/timeRecordService'
import ProjectTabs from '../projects/ProjectTabs'
import TimeRecorder from './TimeRecorder'

type AuthenticatedTimeRecordsProps = {
  timeRecords: TimeRecordService | undefined
  onSignOut: () => void
}

export default function AuthenticatedTimeRecords({ timeRecords, onSignOut }: AuthenticatedTimeRecordsProps) {
  const [state, dispatch] = useReducer(projectTabsReducer, initialProjectTabsState)

  // Preserve the project tabs while the recording service is temporarily unavailable.
  if (!timeRecords) return null

  function signOut() {
    onSignOut()
    dispatch({ type: 'signedOut' })
  }

  return (
    <>
      <button type="button" onClick={signOut}>ログアウト</button>
      <ProjectTabs
        projects={state.projects}
        selectedProject={state.selectedProject}
        onAddProject={() => dispatch({ type: 'dialogOpened' })}
        onSelectProject={project => dispatch({ type: 'projectSelected', project })}
      />
      <TimeRecorder timeRecords={timeRecords} selectedProject={state.selectedProject} />
      {state.showProjectDialog && (
        <div className="project-dialog-overlay">
          <div className="project-dialog" role="dialog" aria-labelledby="project-dialog-title">
            <h2 id="project-dialog-title">プロジェクトを追加</h2>
            <label htmlFor="project-name">プロジェクト名</label>
            <input id="project-name" value={state.projectName} onChange={event => dispatch({ type: 'projectNameChanged', name: event.target.value })} />
            <div className="project-dialog-actions">
              <button className="project-cancel-button" type="button" onClick={() => dispatch({ type: 'dialogCancelled' })}>キャンセル</button>
              <button className="project-register-button" type="button" onClick={() => dispatch({ type: 'projectRegistered' })}>登録</button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
