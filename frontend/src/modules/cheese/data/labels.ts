import type { AdjustmentReason, SaleUnit } from '../types/cheese'

/** Canasta y panela duran de 7 a 11 días; el asadero, 15. */
export const batchShelfLifeDays = 7
export const batchMaxShelfLifeDays = 11
export const purchaseShelfLifeDays = 15
export const expiringSoonDays = 2

export const reasonLabels: Record<AdjustmentReason, string> = {
  waste: 'Merma',
  own_use: 'Consumo propio',
  gift: 'Regalo',
}

export const reasonOptions = (Object.keys(reasonLabels) as AdjustmentReason[]).map((value) => ({ value, label: reasonLabels[value] }))

const kgFormat = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 3 })

/** «9.5 kg», «1 pieza», «12 piezas». */
export function formatQuantity(value: number, unit: SaleUnit): string {
  if (unit === 'kg') return `${kgFormat.format(value)} kg`
  return `${kgFormat.format(value)} ${value === 1 ? 'pieza' : 'piezas'}`
}

export const unitLabel = (unit: SaleUnit) => (unit === 'kg' ? 'kg' : 'piezas')

export function expiryText(days: number): string {
  if (days < -1) return `caducó hace ${-days} días`
  if (days === -1) return 'caducó ayer'
  if (days === 0) return 'caduca hoy'
  if (days === 1) return 'caduca mañana'
  return `caduca en ${days} días`
}

export function expiryTone(days: number): 'danger' | 'warning' | 'success' {
  if (days < 0) return 'danger'
  return days <= expiringSoonDays ? 'warning' : 'success'
}
