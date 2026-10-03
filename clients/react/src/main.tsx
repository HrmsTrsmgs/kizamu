import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { healthApi } from './health/healthApi'
import { timeRecordApi } from './timeRecords/timeRecordApi'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App health={healthApi} timeRecords={timeRecordApi} />
  </StrictMode>,
)
