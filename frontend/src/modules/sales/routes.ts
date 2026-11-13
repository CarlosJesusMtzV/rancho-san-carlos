import type { RouteRecordRaw } from 'vue-router'

export const salesRoutes: RouteRecordRaw[] = [
  { path: 'ventas', name: 'sales', component: () => import('./views/SalesOverviewView.vue'), meta: { title: 'Ventas' } },
  { path: 'ventas/nueva', name: 'sale-new', component: () => import('./views/OrderFormView.vue'), meta: { title: 'Nueva venta' } },
  { path: 'ventas/:id(\\d+)', name: 'sale', component: () => import('./views/OrderFormView.vue'), meta: { title: 'Pedido' } },
  { path: 'ventas/clientes', name: 'customers', component: () => import('./views/CustomersView.vue'), meta: { title: 'Clientes' } },
]
