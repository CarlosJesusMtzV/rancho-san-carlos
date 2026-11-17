import { can, session } from '../shared/auth/session'

// Qué permiso pide cada zona del panel. La regla más específica va primero.
// «write» aplica a las pantallas de alta (rutas con /nuevo o /nueva). Matriz: backend/Modules/Access/specs/access.spec.md.
const zones: { prefix: string; read: string[]; write?: string[] }[] = [
  { prefix: '/panel/usuarios', read: ['users.manage'] },
  { prefix: '/panel/cuenta', read: [] },
  { prefix: '/panel/hato', read: ['herd.read'], write: ['herd.write'] },
  { prefix: '/panel/sanidad', read: ['health.read'], write: ['health.write'] },
  { prefix: '/panel/reproduccion', read: ['repro.read'], write: ['repro.write'] },
  { prefix: '/panel/ordena', read: ['milking.read'] },
  { prefix: '/panel/queseria/compras', read: ['cheese.purchases'] },
  { prefix: '/panel/queseria', read: ['cheese.read'], write: ['cheese.produce'] },
  { prefix: '/panel/matanza', read: ['slaughter.read'], write: ['slaughter.write'] },
  { prefix: '/panel/ventas', read: ['sales.cheese', 'sales.livestock'] },
]

/** ¿El usuario puede abrir esta ruta? Las rutas fuera del panel son públicas. */
export function canVisit(path: string): boolean {
  if (!path.startsWith('/panel')) return true
  if (!session.value) return false
  const zone = zones.find((item) => path === item.prefix || path.startsWith(`${item.prefix}/`))
  if (!zone) return true
  const needed = zone.write && /\/nuev[oa]$/.test(path) ? zone.write : zone.read
  return needed.length === 0 || can(...needed)
}
