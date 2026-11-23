import { useEffect, useRef, useState } from 'react'
import HealthStatus from './health/HealthStatus'
import AuthenticatedTimeRecords from './timeRecords/AuthenticatedTimeRecords'
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
  const [hasSignedOut, setHasSignedOut] = useState(false)
  const googleButton = useRef<HTMLDivElement>(null)
  const activeTimeRecords = signedInTimeRecords ?? (isAuthenticated && !hasSignedOut ? timeRecords : undefined)
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

  function signOut() {
    setHasSignedOut(true)
    setSignedInTimeRecords(undefined)
  }

  return (
    <main>
      <header className="app-header">
        <h1>Kizamu</h1>
        <p className="app-description">作業時間を記録します。</p>
      </header>
      {!activeTimeRecords && (
        renderGoogleButton ? <div ref={googleButton} /> : <button type="button" onClick={signIn}>Googleでログイン</button>
      )}
      <AuthenticatedTimeRecords timeRecords={activeTimeRecords} onSignOut={signOut} />
      <footer className="app-footer">
        <HealthStatus health={health} />
      </footer>
    </main>
  )
}
