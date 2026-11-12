import { createRouter, createWebHistory } from 'vue-router'

import { animalHealthRoutes } from '../../modules/animal-health'
import { herdRoutes } from '../../modules/herd'
import { homeRoutes } from '../../modules/home'
import { reproductionRoutes } from '../../modules/reproduction'
import { landingRoutes } from '../../modules/landing'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // Sitio público: usa su propio encabezado y pie, fuera del layout del sistema.
    ...landingRoutes,
    // Sistema de gestión: los módulos de dominio se agregan como rutas hijas.
    {
      path: '/panel',
      component: () => import('../layouts/MainLayout.vue'),
      children: [...homeRoutes, ...herdRoutes, ...animalHealthRoutes, ...reproductionRoutes],
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

router.afterEach((to) => {
  const title = typeof to.meta.title === 'string' ? to.meta.title : 'Sistema'
  document.title = `${title} | Rancho San Carlos`
})

export default router
