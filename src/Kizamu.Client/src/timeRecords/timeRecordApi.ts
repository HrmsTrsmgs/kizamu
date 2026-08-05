export type StartTimeRecordResponse = {
  id: string
  startedAt: string
}

type EndTimeRecordResponse = {
  id: string
  startedAt: string
  endedAt: string
}

export type TimeRecordApi = {
  getTimeRecords: () => Promise<StartTimeRecordResponse[]>
  startTimeRecord: () => Promise<StartTimeRecordResponse>
  endTimeRecord: (id: string) => Promise<EndTimeRecordResponse>
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
