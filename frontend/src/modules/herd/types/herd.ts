export type SpeciesId = 1 | 2
export type Sex = 'M' | 'F'
export type LifecycleStatus = 'active' | 'sold' | 'slaughtered' | 'dead' | 'culled'
export type ManualExitStatus = 'sold' | 'dead' | 'culled'
export type Origin = 'born_on_ranch' | 'purchased'
export type StatusFilter = 'active' | 'inactive' | 'all'

export interface SpeciesItem {
  id: SpeciesId
  name: string
  productionPurpose: string
}

export interface BreedItem {
  id: number
  speciesId: SpeciesId
  name: string
  purpose: string | null
}

export interface LotItem {
  id: number
  speciesId: SpeciesId
  code: string
  description: string | null
  activeAnimals: number
}

export interface HerdCatalog {
  species: SpeciesItem[]
  breeds: BreedItem[]
  lots: LotItem[]
}

export interface AnimalSummary {
  id: number
  visualTag: string
  officialTag: string | null
  speciesId: SpeciesId
  speciesName: string
  breedName: string
  lotId: number | null
  lotCode: string | null
  sex: Sex
  birthDate: string | null
  ageMonths: number | null
  productionStage: string | null
  lifecycleStatus: LifecycleStatus
  lastWeightKg: number | null
  lastWeighedOn: string | null
}

export interface AnimalPage {
  items: AnimalSummary[]
  total: number
  page: number
  pageSize: number
}

export interface AnimalRef {
  id: number
  visualTag: string
}

export interface WeightItem {
  id: number
  weighedOn: string
  weightKg: number
  notes: string | null
}

export interface AnimalDetail extends AnimalSummary {
  breedId: number
  origin: Origin
  birthWeightKg: number | null
  exitDate: string | null
  geneticsNotes: string | null
  sire: AnimalRef | null
  dam: AnimalRef | null
  weights: WeightItem[]
}

export interface CalfSaleCandidate {
  animalId: number
  visualTag: string
  officialTag: string | null
  sex: Sex
  lotCode: string | null
  birthDate: string
  ageMonths: number
  lastWeightKg: number | null
  lastWeighedOn: string | null
  meatAllowedFrom: string | null
  reachedSaleTarget: boolean
}

export interface AnimalQuery {
  speciesId?: SpeciesId
  lotId?: number
  sex?: Sex
  status?: StatusFilter
  search?: string
  page?: number
  pageSize?: number
}

export interface AnimalInput {
  visualTag: string
  officialTag: string | null
  breedId: number
  lotId: number | null
  sex: Sex
  birthDate: string | null
  productionStage: string | null
  origin: Origin
  birthWeightKg: number | null
  sireId: number | null
  damId: number | null
  geneticsNotes: string | null
}

export interface CreateAnimalInput extends AnimalInput {
  speciesId: SpeciesId
}
