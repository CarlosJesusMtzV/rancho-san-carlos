import type { BrandOptions, NavigationItem } from '../../shared/components/navigation'

// Cambiar estas opciones por aplicación. Header y sidebar son independientes.
export const mainLayoutConfig = {
  showHeader: false,
  showSidebar: true,
  header: { title: 'San Carlos', subtitle: 'Ganadería y quesería', showTitle: true, showLogo: true } satisfies BrandOptions,
  sidebar: { title: 'San Carlos', subtitle: 'Ganadería y quesería', showTitle: true, showLogo: true } satisfies BrandOptions,
}

// Agregar aquí cada módulo cuando su ruta exista; no enlazar vistas pendientes.
export const headerNavigation: NavigationItem[] = [
  { label: 'Panel', to: '/panel' },
  { label: 'Hato', to: '/panel/hato' },
  { label: 'Sanidad', to: '/panel/sanidad' },
  { label: 'Sitio público', to: '/' },
]

export const sidebarNavigation: NavigationItem[] = [
  { label: 'Panel', to: '/panel' },
  {
    label: 'Hato',
    children: [
      { label: 'Animales', to: '/panel/hato' },
      { label: 'Becerros para venta', to: '/panel/hato/becerros' },
      { label: 'Razas y lotes', to: '/panel/hato/catalogos' },
    ],
  },
  {
    label: 'Sanidad',
    children: [
      { label: 'Resumen y retiros', to: '/panel/sanidad' },
      { label: 'Registrar evento', to: '/panel/sanidad/eventos/nuevo' },
      { label: 'Visitas del veterinario', to: '/panel/sanidad/visitas' },
      { label: 'Historial', to: '/panel/sanidad/historial' },
      { label: 'Medicamentos', to: '/panel/sanidad/medicamentos' },
      { label: 'Laboratorio', to: '/panel/sanidad/laboratorio' },
    ],
  },
  { label: 'Sitio público', to: '/' },
]
