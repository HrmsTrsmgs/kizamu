export type TimeRecordResponse = {
  id: string
  startedAt: string
  endedAt: string | null
}

export type TimeRecordApi = {
  getTimeRecords: () => Promise<TimeRecordResponse[]>
  startTimeRecord: () => Promise<TimeRecordResponse>
  endTimeRecord: (id: string) => Promise<TimeRecordResponse>
}

export const timeRecordApi: TimeRecordApi = {
  async getTimeRecords() {
    const response = await fetch('/api/time-records')
    return response.json()
  },
  async startTimeRecord() {
    const response = await fetch('/api/time-records/start', { method: 'POST' })
    return response.json()
  },
  async endTimeRecord(id) {
    const response = await fetch(`/api/time-records/${id}/end`, { method: 'POST' })
    return response.json()
  },
}
