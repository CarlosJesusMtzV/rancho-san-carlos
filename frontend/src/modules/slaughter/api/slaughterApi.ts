import { http } from '../../../shared/api/http'
import type { Carcass, SaveRunInput, SlaughterCandidate, SlaughterRunDetail, SlaughterRunSummary, SlaughterSummary } from '../types/slaughter'

const base = '/slaughter'

export async function listKids(date?: string): Promise<SlaughterCandidate[]> {
  return (await http.get<SlaughterCandidate[]>(`${base}/kids`, { params: { date } })).data
}

export async function listRuns(): Promise<SlaughterRunSummary[]> {
  return (await http.get<SlaughterRunSummary[]>(`${base}/runs`)).data
}

export async function getRun(id: number): Promise<SlaughterRunDetail> {
  return (await http.get<SlaughterRunDetail>(`${base}/runs/${id}`)).data
}

export async function createRun(input: SaveRunInput): Promise<SlaughterRunDetail> {
  return (await http.post<SlaughterRunDetail>(`${base}/runs`, input)).data
}

export async function updateRun(id: number, input: SaveRunInput): Promise<SlaughterRunDetail> {
  return (await http.put<SlaughterRunDetail>(`${base}/runs/${id}`, input)).data
}

export async function deleteRun(id: number): Promise<void> {
  await http.delete(`${base}/runs/${id}`)
}

export async function listCarcasses(): Promise<Carcass[]> {
  return (await http.get<Carcass[]>(`${base}/carcasses`)).data
}

export async function getSummary(): Promise<SlaughterSummary> {
  return (await http.get<SlaughterSummary>(`${base}/summary`)).data
}
