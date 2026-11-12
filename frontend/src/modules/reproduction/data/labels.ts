import type { BreedingOutcome, CheckResult } from '../types/reproduction'

export const outcomeLabels: Record<BreedingOutcome, string> = {
  pending: 'Montada, sin diagnóstico',
  pregnant: 'Cargada',
  open: 'Vacía',
  aborted: 'Aborto',
  delivered: 'Parió',
}

export function outcomeTone(outcome: BreedingOutcome | null): 'success' | 'warning' | 'danger' | 'info' {
  if (outcome === 'pregnant') return 'success'
  if (outcome === 'pending') return 'info'
  if (outcome === 'aborted') return 'danger'
  return 'warning'
}

export const checkResultOptions: { value: CheckResult; label: string }[] = [
  { value: 'pregnant', label: 'Cargada' },
  { value: 'open', label: 'Vacía' },
  { value: 'aborted', label: 'Aborto' },
]

/** Igual que ReproductionService.GestationDays del backend. */
export function gestationDays(speciesId: 1 | 2): number {
  return speciesId === 1 ? 283 : 150
}

export function dueText(daysToDue: number | null): string {
  if (daysToDue === null) return ''
  if (daysToDue === 0) return 'hoy'
  if (daysToDue > 0) return daysToDue === 1 ? 'en 1 día' : `en ${daysToDue} días`
  return daysToDue === -1 ? 'vencido hace 1 día' : `vencido hace ${-daysToDue} días`
}
