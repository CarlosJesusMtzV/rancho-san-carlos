export { animalHealthRoutes } from './routes'
// API pública para otros módulos (Reproducción usa las visitas del veterinario).
export { listVisits } from './api/healthApi'
export type { VetVisitSummary } from './types/health'
