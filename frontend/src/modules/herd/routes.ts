import type { RouteRecordRaw } from 'vue-router'

// Rutas hijas de /panel.
export const herdRoutes: RouteRecordRaw[] = [
  { path: 'hato', name: 'herd', component: () => import('./views/HerdListView.vue'), meta: { title: 'Hato' } },
  { path: 'hato/nuevo', name: 'herd-new', component: () => import('./views/AnimalFormView.vue'), meta: { title: 'Registrar animal' } },
  { path: 'hato/catalogos', name: 'herd-catalog', component: () => import('./views/HerdCatalogView.vue'), meta: { title: 'Razas y lotes' } },
  { path: 'hato/becerros', name: 'herd-calves', component: () => import('./views/CalfSaleView.vue'), meta: { title: 'Becerros para venta' } },
  { path: 'hato/:id(\\d+)', name: 'herd-animal', component: () => import('./views/AnimalDetailView.vue'), meta: { title: 'Ficha del animal' } },
  {
    path: 'hato/:id(\\d+)/editar',
    name: 'herd-edit',
    component: () => import('./views/AnimalFormView.vue'),
    meta: { title: 'Editar animal' },
  },
]
