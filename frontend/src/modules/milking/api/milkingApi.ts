import { http } from '../../../shared/api/http'
import type { MilkDayDetail, MilkDaySummary, MilkingGoat, MilkSummary } from '../types/milking'

const base = '/milking'

export async function listGoats(date?: string): Promise<MilkingGoat[]> {
  return (await http.get<MilkingGoat[]>(`${base}/goats`, { params: { date } })).data
}

export async function listDays(range: { from?: string; to?: string } = {}): Promise<MilkDaySummary[]> {
  return (await http.get<MilkDaySummary[]>(`${base}/days`, { params: range })).data
}

export async function getDay(id: number): Promise<MilkDayDetail> {
  return (await http.get<MilkDayDetail>(`${base}/days/${id}`)).data
}

export async function createDay(input: { collectionDate: string; totalLiters: number; goatIds: number[]; notes: string | null }): Promise<MilkDayDetail> {
  return (await http.post<MilkDayDetail>(`${base}/days`, input)).data
}

export async function updateDay(id: number, input: { totalLiters: number; goatIds: number[]; notes: string | null }): Promise<MilkDayDetail> {
  return (await http.put<MilkDayDetail>(`${base}/days/${id}`, input)).data
}

export async function deleteDay(id: number): Promise<void> {
  await http.delete(`${base}/days/${id}`)
}

export async function getSummary(): Promise<MilkSummary> {
  return (await http.get<MilkSummary>(`${base}/summary`)).data
}
