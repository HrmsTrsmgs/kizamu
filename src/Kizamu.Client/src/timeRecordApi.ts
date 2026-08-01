type StartTimeRecordResponse = {
  id: string
  startedAt: string
}

export type TimeRecordApi = {
  startTimeRecord: () => Promise<StartTimeRecordResponse>
}

export const timeRecordApi: TimeRecordApi = {
  async startTimeRecord() {
    const response = await fetch('/api/time-records/start', { method: 'POST' })
    return response.json()
  },
}
