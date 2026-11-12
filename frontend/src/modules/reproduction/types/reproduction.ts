export type BreedingOutcome = 'pending' | 'pregnant' | 'open' | 'aborted' | 'delivered'
export type CheckResult = 'pregnant' | 'open' | 'aborted'
export type FemaleFilter = 'all' | 'open' | 'pending' | 'pregnant' | 'due-soon'

export interface PregnancyCheckItem {
  id: number
  vetVisitId: number
  vetName: string
  checkDate: string
  result: CheckResult
  notes: string | null
}

export interface BreedingItem {
  id: number
  speciesId: 1 | 2
  femaleId: number
  femaleTag: string
  maleId: number
  maleTag: string
  matingDate: string
  expectedDueDate: string
  outcome: BreedingOutcome
  notes: string | null
  checks: PregnancyCheckItem[]
  birthId: number | null
}

export interface FemaleStatus {
  animalId: number
  visualTag: string
  speciesId: 1 | 2
  breedName: string
  lotCode: string | null
  ageMonths: number | null
  breeding: BreedingItem | null
  lastBirthDate: string | null
  birthCount: number
  daysToDue: number | null
}

export interface OffspringItem {
  animalId: number
  visualTag: string
  sex: 'M' | 'F'
  birthWeightKg: number | null
}

export interface BirthItem {
  id: number
  damId: number
  damTag: string
  speciesId: 1 | 2
  breedingId: number | null
  sireTag: string | null
  birthDate: string
  offspringCount: number
  stillbornCount: number
  complications: string | null
  offspring: OffspringItem[]
}

export interface OffspringInput {
  visualTag: string
  officialTag: string | null
  sex: 'M' | 'F'
  birthWeightKg: number | null
  breedId: number | null
  lotId: number | null
}
