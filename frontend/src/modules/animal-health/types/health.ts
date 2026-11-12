export type HealthEventType = 'vaccination' | 'deworming' | 'illness' | 'injury' | 'checkup' | 'other'
export type LabResult = 'pending' | 'negative' | 'positive' | 'suspect'

export interface SupplyTypeItem {
  id: number
  name: string
  category: string
}

export interface MedicationItem {
  id: number
  supplyTypeId: number
  supplyTypeName: string
  commercialName: string
  activeIngredient: string | null
  defaultMilkWithdrawalDays: number
  defaultMeatWithdrawalDays: number
}

export interface HealthCatalog {
  supplyTypes: SupplyTypeItem[]
  medications: MedicationItem[]
}

export interface TreatmentItem {
  id: number
  supplyId: number
  medicationName: string
  dose: string
  milkWithdrawalDays: number
  meatWithdrawalDays: number
}

export interface HealthEventItem {
  id: number
  animalId: number
  visualTag: string
  speciesId: 1 | 2
  eventDate: string
  eventType: HealthEventType
  diagnosis: string | null
  vetVisitId: number | null
  vetName: string | null
  treatments: TreatmentItem[]
  milkAllowedFrom: string | null
  meatAllowedFrom: string | null
}

export interface HealthEventPage {
  items: HealthEventItem[]
  total: number
  page: number
  pageSize: number
}

export interface VetVisitSummary {
  id: number
  visitDate: string
  vetName: string
  serviceFee: number
  medicationCost: number
  total: number
  notes: string | null
  eventCount: number
  animalCount: number
}

export interface VetVisitDetail extends VetVisitSummary {
  events: HealthEventItem[]
}

export interface ActiveWithdrawal {
  animalId: number
  visualTag: string
  speciesId: 1 | 2
  lotCode: string | null
  milkAllowedFrom: string | null
  meatAllowedFrom: string | null
}

export interface LabTestItem {
  id: number
  animalId: number
  visualTag: string
  sampleDate: string
  testType: string
  labName: string | null
  result: LabResult
}

export interface MedicationInput {
  supplyTypeId: number
  commercialName: string
  activeIngredient: string | null
  defaultMilkWithdrawalDays: number
  defaultMeatWithdrawalDays: number
}

export interface VetVisitInput {
  visitDate: string
  vetName: string
  serviceFee: number
  medicationCost: number
  notes: string | null
}

export interface TreatmentInput {
  supplyId: number
  dose: string
  milkWithdrawalDays: number
  meatWithdrawalDays: number
}

export interface CreateEventsInput {
  animalIds?: number[]
  lotId?: number
  eventDate: string
  eventType: HealthEventType
  diagnosis: string | null
  vetVisitId: number | null
  treatments: TreatmentInput[]
}

export interface HealthEventQuery {
  animalId?: number
  lotId?: number
  visitId?: number
  type?: HealthEventType
  from?: string
  to?: string
  page?: number
  pageSize?: number
}
