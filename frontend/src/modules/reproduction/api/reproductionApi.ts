import { http } from '../../../shared/api/http'
import type { BirthItem, BreedingItem, BreedingOutcome, CheckResult, FemaleFilter, FemaleStatus, OffspringInput } from '../types/reproduction'

const base = '/reproduction'

export async function listFemales(query: { speciesId?: 1 | 2; status?: FemaleFilter } = {}): Promise<FemaleStatus[]> {
  return (await http.get<FemaleStatus[]>(`${base}/females`, { params: query })).data
}

export async function listBreedings(query: { femaleId?: number; outcome?: BreedingOutcome } = {}): Promise<BreedingItem[]> {
  return (await http.get<BreedingItem[]>(`${base}/breedings`, { params: query })).data
}

export async function createBreeding(input: { femaleId: number; maleId: number; matingDate: string; notes: string | null }): Promise<BreedingItem> {
  return (await http.post<BreedingItem>(`${base}/breedings`, input)).data
}

export async function deleteBreeding(id: number): Promise<void> {
  await http.delete(`${base}/breedings/${id}`)
}

export async function addCheck(breedingId: number, input: { vetVisitId: number; result: CheckResult; notes: string | null }): Promise<BreedingItem> {
  return (await http.post<BreedingItem>(`${base}/breedings/${breedingId}/checks`, input)).data
}

export async function listBirths(query: { damId?: number } = {}): Promise<BirthItem[]> {
  return (await http.get<BirthItem[]>(`${base}/births`, { params: query })).data
}

export async function createBirth(input: {
  damId: number
  birthDate: string
  stillbornCount: number
  complications: string | null
  offspring: OffspringInput[]
}): Promise<BirthItem> {
  return (await http.post<BirthItem>(`${base}/births`, input)).data
}
