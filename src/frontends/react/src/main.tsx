import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { healthService } from './infrastructure/healthService'
import { createTimeRecordService } from './infrastructure/timeRecordService'
import { renderGoogleButton } from './googleSignIn'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App health={healthService} createTimeRecords={createTimeRecordService} renderGoogleButton={renderGoogleButton} />
  </StrictMode>,
)
