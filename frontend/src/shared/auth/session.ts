import { isAxiosError } from 'axios'
import { ref } from 'vue'

import { http } from '../api/http'

export interface SessionRole {
  code: string
  name: string
  division: 'general' | 'livestock' | 'cheese'
  description: string
}

export interface Session {
  id: number
  username: string
  fullName: string
  roles: SessionRole[]
  permissions: string[]
  mustChangePassword: boolean
}

/** Sesión del panel. La cookie es HttpOnly: aquí solo vive lo que la API dice del usuario. */
export const session = ref<Session | null>(null)
let loaded = false

/** ¿El usuario tiene alguno de estos permisos? La API vuelve a comprobarlo en cada operación. */
export function can(...anyOf: string[]): boolean {
  const permissions = session.value?.permissions ?? []
  return anyOf.some((permission) => permissions.includes(permission))
}

/** Carga la sesión una vez. Sin sesión (401) devuelve null; otros errores se propagan. */
export async function ensureSession(): Promise<Session | null> {
  if (loaded) return session.value
  try {
    session.value = (await http.get<Session>('/auth/me')).data
  } catch (error) {
    if (!isAxiosError(error) || error.response?.status !== 401) throw error
    session.value = null
  }
  loaded = true
  return session.value
}

export async function login(username: string, password: string): Promise<Session> {
  session.value = (await http.post<Session>('/auth/login', { username, password })).data
  loaded = true
  return session.value
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<Session> {
  session.value = (await http.post<Session>('/auth/change-password', { currentPassword, newPassword })).data
  return session.value
}

export async function logout(): Promise<void> {
  try {
    await http.post('/auth/logout')
  } finally {
    clearSession()
  }
}

/** La sesión dejó de servir (venció o se desactivó la cuenta). */
export function clearSession(): void {
  session.value = null
  loaded = true
}
