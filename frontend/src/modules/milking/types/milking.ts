export interface MilkingGoat {
  animalId: number
  visualTag: string
  productionStage: string | null
  lotCode: string | null
  canBeMilked: boolean
  milkAllowedFrom: string | null
}

export interface MilkDaySummary {
  id: number
  code: string
  collectionDate: string
  totalLiters: number
  goatCount: number
  litersPerGoat: number
  notes: string | null
}

export interface MilkDayDetail extends MilkDaySummary {
  goats: { animalId: number; visualTag: string }[]
  cheeseBatch: { id: number; code: string; litersUsed: number } | null
}

export interface MilkPeriod {
  days: number
  liters: number
  averagePerDay: number
}

export interface MilkSummary {
  lastDay: MilkDaySummary | null
  last7Days: MilkPeriod
  last30Days: MilkPeriod
  previous30Days: MilkPeriod
}
