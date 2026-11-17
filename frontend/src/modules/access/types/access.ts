import type { SessionRole } from '../../../shared/auth/session'

export interface UserItem {
  id: number
  username: string
  fullName: string
  email: string | null
  roles: SessionRole[]
  active: boolean
  mustChangePassword: boolean
  lastLoginAt: string | null
  lockedUntil: string | null
}

export interface UserInput {
  username?: string
  fullName: string
  email: string | null
  roleCodes: string[]
  active?: boolean
  password?: string
}
