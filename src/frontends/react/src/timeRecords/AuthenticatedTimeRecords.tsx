import { useReducer } from 'react'
import { initialProjectTabsState, projectTabsReducer } from '../presentation/projectTabsState'
import type { TimeRecordService } from '../presentation/timeRecordService'
import ProjectTabs from '../projects/ProjectTabs'
import { useTimeRecorder } from '../presentation/useTimeRecorder'
import { TimeRecorderView } from './TimeRecorder'

type AuthenticatedTimeRecordsProps = {
  timeRecords: TimeRecordService | undefined
  onSignOut: () => void
}

export default function AuthenticatedTimeRecords({ timeRecords, onSignOut }: AuthenticatedTimeRecordsProps) {
  const [tabs, dispatch] = useReducer(projectTabsReducer, initialProjectTabsState)
  const { selectProject, ...recorder } = useTimeRecorder(timeRecords)

  // Preserve the project tabs while the recording service is temporarily unavailable.
  if (!timeRecords) return null

  function signOut() {
    onSignOut()
    dispatch({ type: 'signedOut' })
    selectProject(undefined)
  }

  return (
    <>
      <button type="button" onClick={signOut}>ログアウト</button>
      <ProjectTabs
        projects={tabs.projects}
        selectedProject={recorder.state.selectedProject}
        onAddProject={() => dispatch({ type: 'dialogOpened' })}
        onSelectProject={selectProject}
      />
      <TimeRecorderView {...recorder} />
      {tabs.showProjectDialog && (
        <div className="project-dialog-overlay">
          <div className="project-dialog" role="dialog" aria-labelledby="project-dialog-title">
            <h2 id="project-dialog-title">プロジェクトを追加</h2>
            <label htmlFor="project-name">プロジェクト名</label>
            <input id="project-name" value={tabs.projectName} onChange={event => dispatch({ type: 'projectNameChanged', name: event.target.value })} />
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
