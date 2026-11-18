import { useEffect, useReducer, useRef, useState } from 'react'
import HealthStatus from './health/HealthStatus'
import TimeRecorder from './timeRecords/TimeRecorder'
import { initialProjectTabsState, projectTabsReducer } from './presentation/projectTabsState'
import type { HealthService } from './presentation/healthService'
import type { TimeRecordService } from './presentation/timeRecordService'
import './index.css'

type AppProps = {
  isAuthenticated?: boolean
  health: HealthService
  timeRecords?: TimeRecordService
  signInWithGoogle?: () => Promise<string>
  createTimeRecords?: (token: string) => TimeRecordService
  renderGoogleButton?: (container: HTMLElement, onCredential: (token: string) => void) => void
}

export default function App({ isAuthenticated = false, health, timeRecords, signInWithGoogle, createTimeRecords, renderGoogleButton }: AppProps) {
  const [signedInTimeRecords, setSignedInTimeRecords] = useState<TimeRecordService>()
  const googleButton = useRef<HTMLDivElement>(null)
  const activeTimeRecords = signedInTimeRecords ?? (isAuthenticated ? timeRecords : undefined)
  useEffect(() => {
    if (googleButton.current && renderGoogleButton && createTimeRecords) {
      renderGoogleButton(googleButton.current, token => setSignedInTimeRecords(createTimeRecords(token)))
    }
  }, [renderGoogleButton, createTimeRecords, activeTimeRecords])

  async function signIn() {
    if (signInWithGoogle && createTimeRecords) {
      const token = await signInWithGoogle()
      setSignedInTimeRecords(createTimeRecords(token))
    }
  }
  const [state, dispatch] = useReducer(projectTabsReducer, initialProjectTabsState)

  function signOut() {
    setSignedInTimeRecords(undefined)
    dispatch({ type: 'signedOut' })
  }

  return (
    <main>
      <header className="app-header">
        <h1>Kizamu</h1>
        <p className="app-description">作業時間を記録します。</p>
      </header>
      {!activeTimeRecords ? (renderGoogleButton ? <div ref={googleButton} /> : <button type="button" onClick={signIn}>Googleでログイン</button>) : <>
        <button type="button" onClick={signOut}>ログアウト</button>
        <section className="project-toolbar" aria-labelledby="projects-title">
          <div className="project-toolbar-heading">
            <h2 id="projects-title">プロジェクト</h2>
            <button className="project-add-button" type="button" onClick={() => dispatch({ type: 'dialogOpened' })}>追加</button>
          </div>
          <div className="project-tabs" role="tablist" aria-label="プロジェクト">
            <span className="project-default" aria-selected={state.selectedProject === undefined}>未指定</span>
            {state.projects.map((project, index) => (
              <button
                className="project-tab"
                role="tab"
                aria-selected={state.selectedProject === project}
                type="button"
                key={index}
                onClick={() => dispatch({ type: 'projectSelected', project })}
              >{project.name}</button>
            ))}
          </div>
        </section>
        <TimeRecorder timeRecords={activeTimeRecords} selectedProject={state.selectedProject} />
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
      </>}
      <footer className="app-footer">
        <HealthStatus health={health} />
      </footer>
    </main>
  )
}
