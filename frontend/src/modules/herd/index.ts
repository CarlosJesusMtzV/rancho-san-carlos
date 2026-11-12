export { herdRoutes } from './routes'
// API pública para otros módulos (Sanidad, Ordeña, Ventas).
export { getCatalog as getHerdCatalog, listAnimals } from './api/herdApi'
export { default as SpeciesTag } from './components/SpeciesTag.vue'
export { speciesLabels } from './data/labels'
export type { AnimalSummary, BreedItem, LotItem, SpeciesId } from './types/herd'
