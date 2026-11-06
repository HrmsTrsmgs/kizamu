import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { healthService } from './infrastructure/healthService'
import { timeRecordService } from './infrastructure/timeRecordService'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App health={healthService} timeRecords={timeRecordService} />
  </StrictMode>,
)
