import { http } from '../../../shared/api/http'
import type {
  AnimalDetail,
  AnimalInput,
  AnimalPage,
  AnimalQuery,
  BreedItem,
  CalfSaleCandidate,
  CreateAnimalInput,
  HerdCatalog,
  LotItem,
  ManualExitStatus,
  SpeciesId,
  WeightItem,
} from '../types/herd'

const base = '/herd'

export async function getCatalog(): Promise<HerdCatalog> {
  return (await http.get<HerdCatalog>(`${base}/catalog`)).data
}

export async function createBreed(input: { speciesId: SpeciesId; name: string; purpose: string | null }): Promise<BreedItem> {
  return (await http.post<BreedItem>(`${base}/breeds`, input)).data
}

export async function createLot(input: { speciesId: SpeciesId; code: string; description: string | null }): Promise<LotItem> {
  return (await http.post<LotItem>(`${base}/lots`, input)).data
}

export async function listAnimals(query: AnimalQuery): Promise<AnimalPage> {
  return (await http.get<AnimalPage>(`${base}/animals`, { params: query })).data
}

export async function getAnimal(id: number): Promise<AnimalDetail> {
  return (await http.get<AnimalDetail>(`${base}/animals/${id}`)).data
}

export async function createAnimal(input: CreateAnimalInput): Promise<AnimalDetail> {
  return (await http.post<AnimalDetail>(`${base}/animals`, input)).data
}

export async function updateAnimal(id: number, input: AnimalInput): Promise<AnimalDetail> {
  return (await http.put<AnimalDetail>(`${base}/animals/${id}`, input)).data
}

export async function registerExit(id: number, input: { status: ManualExitStatus; exitDate: string }): Promise<AnimalDetail> {
  return (await http.post<AnimalDetail>(`${base}/animals/${id}/exit`, input)).data
}

export async function addWeight(id: number, input: { weighedOn: string; weightKg: number; notes: string | null }): Promise<WeightItem> {
  return (await http.post<WeightItem>(`${base}/animals/${id}/weights`, input)).data
}

export async function deleteWeight(id: number, weightId: number): Promise<void> {
  await http.delete(`${base}/animals/${id}/weights/${weightId}`)
}

export async function getCalfSaleCandidates(): Promise<CalfSaleCandidate[]> {
  return (await http.get<CalfSaleCandidate[]>(`${base}/calf-sale-candidates`)).data
}
