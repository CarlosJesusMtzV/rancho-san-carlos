import type { RouteRecordRaw } from 'vue-router'

export const cheeseRoutes: RouteRecordRaw[] = [
  { path: 'queseria', name: 'cheese', component: () => import('./views/CheeseOverviewView.vue'), meta: { title: 'Quesería' } },
  { path: 'queseria/tandas/nueva', name: 'cheese-batch-new', component: () => import('./views/CheeseBatchFormView.vue'), meta: { title: 'Registrar tanda' } },
  { path: 'queseria/tandas/:id(\\d+)', name: 'cheese-batch', component: () => import('./views/CheeseBatchFormView.vue'), meta: { title: 'Tanda de queso' } },
  { path: 'queseria/compras/nueva', name: 'cheese-purchase-new', component: () => import('./views/CheesePurchaseFormView.vue'), meta: { title: 'Comprar asadero' } },
  { path: 'queseria/compras/:id(\\d+)', name: 'cheese-purchase', component: () => import('./views/CheesePurchaseFormView.vue'), meta: { title: 'Compra de asadero' } },
  { path: 'queseria/historial', name: 'cheese-history', component: () => import('./views/CheeseHistoryView.vue'), meta: { title: 'Historial de quesería' } },
]
