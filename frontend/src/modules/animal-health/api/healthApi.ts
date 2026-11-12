import { http } from '../../../shared/api/http'
import type {
  ActiveWithdrawal,
  CreateEventsInput,
  HealthCatalog,
  HealthEventItem,
  HealthEventPage,
  HealthEventQuery,
  LabResult,
  LabTestItem,
  MedicationInput,
  MedicationItem,
  VetVisitDetail,
  VetVisitInput,
  VetVisitSummary,
} from '../types/health'

const base = '/animal-health'

export async function getHealthCatalog(): Promise<HealthCatalog> {
  return (await http.get<HealthCatalog>(`${base}/catalog`)).data
}

export async function createMedication(input: MedicationInput): Promise<MedicationItem> {
  return (await http.post<MedicationItem>(`${base}/medications`, input)).data
}

export async function updateMedication(id: number, input: MedicationInput): Promise<MedicationItem> {
  return (await http.put<MedicationItem>(`${base}/medications/${id}`, input)).data
}

export async function listVisits(range: { from?: string; to?: string } = {}): Promise<VetVisitSummary[]> {
  return (await http.get<VetVisitSummary[]>(`${base}/visits`, { params: range })).data
}

export async function getVisit(id: number): Promise<VetVisitDetail> {
  return (await http.get<VetVisitDetail>(`${base}/visits/${id}`)).data
}

export async function createVisit(input: VetVisitInput): Promise<VetVisitDetail> {
  return (await http.post<VetVisitDetail>(`${base}/visits`, input)).data
}

export async function updateVisit(id: number, input: VetVisitInput): Promise<VetVisitDetail> {
  return (await http.put<VetVisitDetail>(`${base}/visits/${id}`, input)).data
}

export async function listEvents(query: HealthEventQuery): Promise<HealthEventPage> {
  return (await http.get<HealthEventPage>(`${base}/events`, { params: query })).data
}

export async function createEvents(input: CreateEventsInput): Promise<{ created: number; events: HealthEventItem[] }> {
  return (await http.post<{ created: number; events: HealthEventItem[] }>(`${base}/events`, input)).data
}

export async function deleteEvent(id: number): Promise<void> {
  await http.delete(`${base}/events/${id}`)
}

export async function listWithdrawals(): Promise<ActiveWithdrawal[]> {
  return (await http.get<ActiveWithdrawal[]>(`${base}/withdrawals`)).data
}

export async function listLabTests(query: { animalId?: number; result?: LabResult } = {}): Promise<LabTestItem[]> {
  return (await http.get<LabTestItem[]>(`${base}/lab-tests`, { params: query })).data
}

export async function createLabTest(input: { animalId: number; sampleDate: string; testType: string; labName: string | null }): Promise<LabTestItem> {
  return (await http.post<LabTestItem>(`${base}/lab-tests`, input)).data
}

export async function setLabResult(id: number, result: LabResult): Promise<LabTestItem> {
  return (await http.put<LabTestItem>(`${base}/lab-tests/${id}/result`, { result })).data
}
