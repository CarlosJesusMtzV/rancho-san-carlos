import { createRouter, createWebHistory } from 'vue-router'

import { clearSession, ensureSession } from '../../shared/auth/session'
import { http } from '../../shared/api/http'
import { canVisit } from '../access'

import { animalHealthRoutes } from '../../modules/animal-health'
import { herdRoutes } from '../../modules/herd'
import { homeRoutes } from '../../modules/home'
import { cheeseRoutes } from '../../modules/cheese'
import { milkingRoutes } from '../../modules/milking'
import { reproductionRoutes } from '../../modules/reproduction'
import { salesRoutes } from '../../modules/sales'
import { slaughterRoutes } from '../../modules/slaughter'
import { landingRoutes } from '../../modules/landing'
import { accessPanelRoutes, loginRoute } from '../../modules/access'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // Sitio público: usa su propio encabezado y pie, fuera del layout del sistema.
    ...landingRoutes,
    loginRoute,
    // Sistema de gestión: los módulos de dominio se agregan como rutas hijas.
    {
      path: '/panel',
      component: () => import('../layouts/MainLayout.vue'),
      children: [...homeRoutes, ...herdRoutes, ...animalHealthRoutes, ...reproductionRoutes, ...milkingRoutes, ...cheeseRoutes, ...slaughterRoutes, ...salesRoutes, ...accessPanelRoutes],
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('../../shared/views/NotFoundView.vue'),
    },
  ],
  scrollBehavior(to, _from, savedPosition) {
    return savedPosition ?? (to.hash ? { el: to.hash, top: 24 } : { top: 0 })
  },
})

// El panel exige sesión; cada zona, el permiso de su módulo. La API lo vuelve a comprobar.
router.beforeEach(async (to) => {
  const inPanel = to.path.startsWith('/panel')
  if (!inPanel && to.name !== 'login') return true

  let current = null
  try {
    current = await ensureSession()
  } catch {
    // Sin conexión con la API: la pantalla de entrada lo explica al intentar entrar.
  }

  if (to.name === 'login') return current ? { path: '/panel' } : true
  if (!current) return { name: 'login', query: to.fullPath === '/panel' ? {} : { redirect: to.fullPath } }
  if (current.mustChangePassword && to.name !== 'account') return { name: 'account' }
  if (!canVisit(to.path)) return { path: '/panel', query: { 'sin-permiso': '1' } }
  return true
})

// Si la sesión vence o la cuenta se desactiva, cualquier respuesta 401 regresa a la pantalla de entrada.
http.interceptors.response.use(undefined, (error) => {
  const url: string = error?.config?.url ?? ''
  if (error?.response?.status === 401 && !url.startsWith('/auth/')) {
    clearSession()
    const current = router.currentRoute.value
    if (current.name !== 'login') void router.push({ name: 'login', query: { redirect: current.fullPath, vencida: '1' } })
  }
  return Promise.reject(error)
})

router.afterEach((to) => {
  const title = typeof to.meta.title === 'string' ? to.meta.title : 'Sistema'
  document.title = `${title} | Rancho San Carlos`
})

export default router
