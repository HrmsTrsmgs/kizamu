import { useProjectTabs } from '../presentation/useProjectTabs'
import type { TimeRecordService } from '../presentation/timeRecordService'
import ProjectRegistrationDialog from '../projects/ProjectRegistrationDialog'
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
        <ProjectRegistrationDialog
          projectName={registration.projectName}
          onProjectNameChange={registration.changeName}
          onRegister={registration.register}
          onCancel={registration.cancel}
        />
      )}
    </>
  )
}
