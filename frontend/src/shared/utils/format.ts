const dateFormat = new Intl.DateTimeFormat('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })
const moneyFormat = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', minimumFractionDigits: 2 })

/** Fecha ISO (YYYY-MM-DD) a DD/MM/AAAA, sin desfase de zona horaria. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-').map(Number)
  return dateFormat.format(new Date(Date.UTC(year, month - 1, day)))
}

/** Importe en pesos mexicanos. */
export function formatMoney(value: number | null | undefined): string {
  return value === null || value === undefined ? '—' : moneyFormat.format(value)
}

/** Fecha de hoy en la zona del rancho (Coahuila), formato YYYY-MM-DD. */
export function todayIso(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Monterrey' }).format(new Date())
}

/** Suma días a una fecha ISO. */
export function addDays(value: string, days: number): string {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1, day + days)).toISOString().slice(0, 10)
}
