import { useProjectTabs } from '../presentation/useProjectTabs'
import type { TimeRecordService } from '../presentation/timeRecordService'
import ProjectTabs from '../projects/ProjectTabs'
import { useTimeRecorder } from '../presentation/useTimeRecorder'
import { TimeRecorderView } from './TimeRecorder'

type AuthenticatedTimeRecordsProps = {
  timeRecords: TimeRecordService | undefined
  onSignOut: () => void
}

export default function AuthenticatedTimeRecords({ timeRecords, onSignOut }: AuthenticatedTimeRecordsProps) {
  const { projects, registration, reset: resetProjectTabs } = useProjectTabs()
  const { selectProject, ...recorder } = useTimeRecorder(timeRecords)

  // Preserve the project tabs while the recording service is temporarily unavailable.
  if (!timeRecords) return null

  function signOut() {
    onSignOut()
    resetProjectTabs()
    selectProject(undefined)
  }

  return (
    <>
      <button type="button" onClick={signOut}>ログアウト</button>
      <ProjectTabs
        projects={projects}
        selectedProject={recorder.state.selectedProject}
        onAddProject={registration.open}
        onSelectProject={selectProject}
      />
      <TimeRecorderView {...recorder} />
      {registration.isOpen && (
        <div className="project-dialog-overlay">
          <div className="project-dialog" role="dialog" aria-labelledby="project-dialog-title">
            <h2 id="project-dialog-title">プロジェクトを追加</h2>
            <label htmlFor="project-name">プロジェクト名</label>
            <input id="project-name" value={registration.projectName} onChange={event => registration.changeName(event.target.value)} />
            <div className="project-dialog-actions">
              <button className="project-cancel-button" type="button" onClick={registration.cancel}>キャンセル</button>
              <button className="project-register-button" type="button" onClick={registration.register}>登録</button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
