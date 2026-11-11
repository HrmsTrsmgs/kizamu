import HealthStatus from './health/HealthStatus'
import TimeRecorder from './timeRecords/TimeRecorder'
import { useProjectTabs } from './presentation/useProjectTabs'
import type { HealthService } from './presentation/healthService'
import type { TimeRecordService } from './presentation/timeRecordService'
import './index.css'

type AppProps = {
  health: HealthService
  timeRecords: TimeRecordService
}

export default function App({ health, timeRecords }: AppProps) {
  const { state, openProjectDialog } = useProjectTabs()

  return (
    <main>
      <header className="app-header">
        <h1>Kizamu</h1>
        <p className="app-description">作業時間を記録します。</p>
      </header>
      <section className="project-toolbar" aria-labelledby="projects-title">
        <div className="project-toolbar-heading">
          <h2 id="projects-title">プロジェクト</h2>
          <button className="project-add-button" type="button" onClick={openProjectDialog}>追加</button>
        </div>
      </section>
      <TimeRecorder timeRecords={timeRecords} />
      {state.showProjectDialog && (
        <div className="project-dialog-overlay">
          <div className="project-dialog" role="dialog" aria-labelledby="project-dialog-title">
            <h2 id="project-dialog-title">プロジェクトを追加</h2>
          </div>
        </div>
      )}
      <footer className="app-footer">
        <HealthStatus health={health} />
      </footer>
    </main>
  )
}
