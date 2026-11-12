import type { RouteRecordRaw } from 'vue-router'

// Rutas hijas de /panel.
export const animalHealthRoutes: RouteRecordRaw[] = [
  { path: 'sanidad', name: 'health', component: () => import('./views/HealthOverviewView.vue'), meta: { title: 'Sanidad' } },
  { path: 'sanidad/eventos/nuevo', name: 'health-event-new', component: () => import('./views/HealthEventFormView.vue'), meta: { title: 'Registrar evento sanitario' } },
  { path: 'sanidad/historial', name: 'health-history', component: () => import('./views/HealthHistoryView.vue'), meta: { title: 'Historial sanitario' } },
  { path: 'sanidad/visitas', name: 'health-visits', component: () => import('./views/VetVisitListView.vue'), meta: { title: 'Visitas del veterinario' } },
  { path: 'sanidad/visitas/:id(\\d+)', name: 'health-visit', component: () => import('./views/VetVisitDetailView.vue'), meta: { title: 'Visita del veterinario' } },
  { path: 'sanidad/medicamentos', name: 'health-medications', component: () => import('./views/MedicationsView.vue'), meta: { title: 'Medicamentos' } },
  { path: 'sanidad/laboratorio', name: 'health-lab', component: () => import('./views/LabTestsView.vue'), meta: { title: 'Pruebas de laboratorio' } },
]
