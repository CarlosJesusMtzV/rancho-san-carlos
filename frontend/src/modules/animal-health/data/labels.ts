import type { HealthEventType, LabResult } from '../types/health'

export const eventTypeLabels: Record<HealthEventType, string> = {
  vaccination: 'Vacunación',
  deworming: 'Desparasitación',
  illness: 'Enfermedad',
  injury: 'Lesión',
  checkup: 'Revisión',
  other: 'Otro',
}

export const eventTypeOptions = Object.entries(eventTypeLabels).map(([value, label]) => ({ value, label }))

/** Tipos que exigen al menos un medicamento (igual que la regla S5 del backend). */
export const typesRequiringTreatment: HealthEventType[] = ['vaccination', 'deworming']

export const labResultLabels: Record<LabResult, string> = {
  pending: 'Pendiente',
  negative: 'Negativo',
  positive: 'Positivo',
  suspect: 'Sospechoso',
}

export const labResultOptions = Object.entries(labResultLabels).map(([value, label]) => ({ value, label }))

export function labResultTone(result: LabResult): 'success' | 'warning' | 'danger' | 'info' {
  return result === 'negative' ? 'success' : result === 'positive' ? 'danger' : result === 'suspect' ? 'warning' : 'info'
}
