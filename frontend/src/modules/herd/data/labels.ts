import type { LifecycleStatus, ManualExitStatus, Origin, Sex, SpeciesId } from '../types/herd'

export const speciesLabels: Record<SpeciesId, string> = { 1: 'Bovino', 2: 'Caprino' }

export const sexLabels: Record<Sex, string> = { M: 'Macho', F: 'Hembra' }

export const originLabels: Record<Origin, string> = { born_on_ranch: 'Nacido en el rancho', purchased: 'Comprado' }

export const lifecycleLabels: Record<LifecycleStatus, string> = {
  active: 'Activo',
  sold: 'Vendido',
  slaughtered: 'Sacrificado',
  dead: 'Muerto',
  culled: 'Desecho',
}

export const exitOptions: { value: ManualExitStatus; label: string }[] = [
  { value: 'sold', label: 'Vendido' },
  { value: 'dead', label: 'Muerto' },
  { value: 'culled', label: 'Desecho' },
]

interface StageOption {
  speciesId: SpeciesId
  code: string
  label: string
  sexes: Sex[]
}

// Debe coincidir con HerdRules.Stages del backend.
export const stages: StageOption[] = [
  { speciesId: 1, code: 'calf', label: 'Becerro / becerra', sexes: ['M', 'F'] },
  { speciesId: 1, code: 'steer', label: 'Novillo', sexes: ['M'] },
  { speciesId: 1, code: 'heifer', label: 'Vaquilla', sexes: ['F'] },
  { speciesId: 1, code: 'cow', label: 'Vaca', sexes: ['F'] },
  { speciesId: 1, code: 'bull', label: 'Toro', sexes: ['M'] },
  { speciesId: 2, code: 'kid', label: 'Cabrito / cabrita', sexes: ['M', 'F'] },
  { speciesId: 2, code: 'doeling', label: 'Primala', sexes: ['F'] },
  { speciesId: 2, code: 'milking_doe', label: 'Cabra en ordeña', sexes: ['F'] },
  { speciesId: 2, code: 'dry_doe', label: 'Cabra seca', sexes: ['F'] },
  { speciesId: 2, code: 'buck', label: 'Semental', sexes: ['M'] },
]

export function stageLabel(code: string | null): string {
  if (!code) return '—'
  return stages.find((stage) => stage.code === code)?.label ?? code
}

export function stagesFor(speciesId: SpeciesId, sex: Sex | ''): StageOption[] {
  return stages.filter((stage) => stage.speciesId === speciesId && (sex === '' || stage.sexes.includes(sex)))
}

const dateFormat = new Intl.DateTimeFormat('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })

/** Fecha ISO (YYYY-MM-DD) a DD/MM/AAAA, sin desfase de zona horaria. */
export function formatDate(value: string | null): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-').map(Number)
  return dateFormat.format(new Date(Date.UTC(year, month - 1, day)))
}

export function formatKg(value: number | null): string {
  if (value === null) return '—'
  return `${value.toLocaleString('es-MX', { maximumFractionDigits: 2 })} kg`
}

export function formatAge(months: number | null): string {
  if (months === null) return '—'
  if (months < 12) return months === 1 ? '1 mes' : `${months} meses`
  const years = Math.floor(months / 12)
  const rest = months % 12
  const yearsText = years === 1 ? '1 año' : `${years} años`
  return rest === 0 ? yearsText : `${yearsText} ${rest} m`
}

/** Fecha de hoy en la zona del rancho, formato YYYY-MM-DD. */
export function todayIso(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Monterrey' }).format(new Date())
}
