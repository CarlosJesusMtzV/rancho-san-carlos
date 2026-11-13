import type { RouteRecordRaw } from 'vue-router'

export const slaughterRoutes: RouteRecordRaw[] = [
  { path: 'matanza', name: 'slaughter', component: () => import('./views/SlaughterOverviewView.vue'), meta: { title: 'Matanza de cabritos' } },
  { path: 'matanza/nueva', name: 'slaughter-new', component: () => import('./views/SlaughterRunFormView.vue'), meta: { title: 'Registrar matanza' } },
  { path: 'matanza/:id(\\d+)', name: 'slaughter-run', component: () => import('./views/SlaughterRunFormView.vue'), meta: { title: 'Matanza' } },
]
