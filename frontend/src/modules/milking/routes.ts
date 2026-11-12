import type { RouteRecordRaw } from 'vue-router'

// Rutas hijas de /panel.
export const milkingRoutes: RouteRecordRaw[] = [
  { path: 'ordena', name: 'milking', component: () => import('./views/MilkingView.vue'), meta: { title: 'Ordeña' } },
  { path: 'ordena/:id(\\d+)', name: 'milking-day', component: () => import('./views/MilkDayView.vue'), meta: { title: 'Ordeña del día' } },
]
