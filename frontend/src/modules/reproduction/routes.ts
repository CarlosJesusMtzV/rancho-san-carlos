import type { RouteRecordRaw } from 'vue-router'

// Rutas hijas de /panel.
export const reproductionRoutes: RouteRecordRaw[] = [
  { path: 'reproduccion', name: 'reproduction', component: () => import('./views/ReproductionOverviewView.vue'), meta: { title: 'Reproducción' } },
  { path: 'reproduccion/empadres/nuevo', name: 'empadre-new', component: () => import('./views/EmpadreFormView.vue'), meta: { title: 'Registrar empadre' } },
  { path: 'reproduccion/partos/nuevo', name: 'birth-new', component: () => import('./views/BirthFormView.vue'), meta: { title: 'Registrar parto' } },
  { path: 'reproduccion/historial', name: 'reproduction-history', component: () => import('./views/ReproductionHistoryView.vue'), meta: { title: 'Historial reproductivo' } },
]
