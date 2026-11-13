export interface SlaughterCandidate {
  animalId: number
  visualTag: string
  sex: 'M' | 'F'
  ageMonths: number | null
  productionStage: string | null
  lastWeightKg: number | null
  canBeSlaughtered: boolean
  meatAllowedFrom: string | null
}

export interface SlaughteredKid {
  slaughterId: number
  animalId: number
  visualTag: string
  sex: 'M' | 'F'
  ageMonths: number | null
  liveWeightKg: number
  carcassWeightKg: number
  yieldPercent: number
  sold: boolean
}

export interface SlaughterRunSummary {
  id: number
  runDate: string
  slaughterhouse: string
  cost: number | null
  kidCount: number
  liveWeightKg: number
  carcassWeightKg: number
  yieldPercent: number
  costPerKid: number | null
  notes: string | null
}

export interface SlaughterRunDetail extends SlaughterRunSummary {
  kids: SlaughteredKid[]
}

export interface Carcass {
  slaughterId: number
  animalId: number
  visualTag: string
  runId: number
  runDate: string
  slaughterhouse: string
  carcassWeightKg: number
  daysSinceSlaughter: number
}

export interface SlaughterSummary {
  availableCarcasses: number
  availableCarcassKg: number
  kidsLast30Days: number
  averageYieldPercent: number | null
  lastRun: SlaughterRunSummary | null
}

export interface SaveRunInput {
  runDate?: string
  slaughterhouse: string
  cost: number | null
  notes: string | null
  kids: { animalId: number; liveWeightKg: number; carcassWeightKg: number }[]
}
