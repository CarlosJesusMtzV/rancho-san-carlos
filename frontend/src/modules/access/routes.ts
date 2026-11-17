import type { RouteRecordRaw } from 'vue-router'

/** Pantalla de entrada: fuera del panel y de su menú. */
export const loginRoute: RouteRecordRaw = {
  path: '/entrar',
  name: 'login',
  component: () => import('./views/LoginView.vue'),
  meta: { title: 'Entrar' },
}

export const accessPanelRoutes: RouteRecordRaw[] = [
  { path: 'cuenta', name: 'account', component: () => import('./views/AccountView.vue'), meta: { title: 'Mi cuenta' } },
  { path: 'usuarios', name: 'users', component: () => import('./views/UsersView.vue'), meta: { title: 'Usuarios' } },
]
