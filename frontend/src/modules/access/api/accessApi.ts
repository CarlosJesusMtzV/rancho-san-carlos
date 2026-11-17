import { http } from '../../../shared/api/http'
import type { SessionRole } from '../../../shared/auth/session'
import type { UserInput, UserItem } from '../types/access'

export async function listUsers(): Promise<UserItem[]> {
  return (await http.get<UserItem[]>('/users')).data
}

export async function listRoles(): Promise<SessionRole[]> {
  return (await http.get<SessionRole[]>('/users/roles')).data
}

export async function createUser(input: UserInput): Promise<UserItem> {
  return (await http.post<UserItem>('/users', input)).data
}

export async function updateUser(id: number, input: UserInput): Promise<UserItem> {
  return (await http.put<UserItem>(`/users/${id}`, input)).data
}

export async function resetPassword(id: number, password: string): Promise<UserItem> {
  return (await http.post<UserItem>(`/users/${id}/reset-password`, { password })).data
}
