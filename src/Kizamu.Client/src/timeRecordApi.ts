type StartTimeRecordResponse = {
  id: string
  startedAt: string
}

export type TimeRecordApi = {
  startTimeRecord: () => Promise<StartTimeRecordResponse>
  endTimeRecord: (id: string) => Promise<void>
}

export const timeRecordApi: TimeRecordApi = {
  async startTimeRecord() {
    const response = await fetch('/api/time-records/start', { method: 'POST' })
    return response.json()
  },
  async endTimeRecord(id) {
    await fetch(`/api/time-records/${id}/end`, { method: 'POST' })
  },
}
