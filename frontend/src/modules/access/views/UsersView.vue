<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { session, type SessionRole } from '../../../shared/auth/session'
import { UiAlert, UiBadge, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiTable, UiToast, UiToggle } from '../../../shared/components'
import { createUser, listRoles, listUsers, resetPassword, updateUser } from '../api/accessApi'
import type { UserItem } from '../types/access'

const toast = ref<InstanceType<typeof UiToast> | null>(null)
const users = ref<UserItem[] | null>(null)
const roles = ref<SessionRole[]>([])
const error = ref('')

const dialogOpen = ref(false)
const editing = ref<UserItem | null>(null)
const form = reactive({ username: '', fullName: '', email: '', password: '', active: true, roleCodes: [] as string[] })
const saveError = ref('')
const saving = ref(false)

const resetOpen = ref(false)
const resetTarget = ref<UserItem | null>(null)
const resetValue = ref('')
const resetError = ref('')

const divisions = [
  { key: 'general' as const, label: 'General' },
  { key: 'livestock' as const, label: 'Ganadería' },
  { key: 'cheese' as const, label: 'Quesería' },
]
const rolesByDivision = computed(() => divisions.map((division) => ({ ...division, roles: roles.value.filter((role) => role.division === division.key) })))
const columns = [
  { key: 'fullName' as const, label: 'Nombre' },
  { key: 'username' as const, label: 'Usuario' },
  { key: 'roles' as const, label: 'Roles' },
  { key: 'active' as const, label: 'Estado' },
  { key: 'lastLoginAt' as const, label: 'Último acceso' },
  { key: 'id' as const, label: 'Acciones' },
]
const dateTime = (value: string | null) =>
  value ? new Date(value).toLocaleString('es-MX', { dateStyle: 'short', timeStyle: 'short' }) : 'Nunca'

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[users.value, roles.value] = await Promise.all([listUsers(), listRoles()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

function open(user: UserItem | null): void {
  editing.value = user
  Object.assign(form, {
    username: user?.username ?? '',
    fullName: user?.fullName ?? '',
    email: user?.email ?? '',
    password: '',
    active: user?.active ?? true,
    roleCodes: user ? user.roles.map((role) => role.code) : [],
  })
  saveError.value = ''
  dialogOpen.value = true
}

function toggleRole(code: string, checked: boolean): void {
  form.roleCodes = checked ? [...form.roleCodes, code] : form.roleCodes.filter((item) => item !== code)
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = !form.fullName.trim() ? 'Escribe el nombre completo.' : form.roleCodes.length === 0 ? 'Elige al menos un rol.' : ''
  if (!saveError.value && !editing.value && (!form.username.trim() || !form.password)) saveError.value = 'Escribe el usuario y una contraseña temporal.'
  if (saveError.value) return
  saving.value = true
  const base = { fullName: form.fullName.trim(), email: form.email.trim() || null, roleCodes: form.roleCodes }
  try {
    const saved = editing.value
      ? await updateUser(editing.value.id, { ...base, active: form.active })
      : await createUser({ ...base, username: form.username.trim(), password: form.password })
    dialogOpen.value = false
    toast.value?.show(editing.value ? `Cuenta de ${saved.fullName} actualizada.` : `Cuenta ${saved.username} creada. Dale su contraseña temporal: la cambiará al entrar.`)
    await load()
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

function openReset(user: UserItem): void {
  resetTarget.value = user
  resetValue.value = ''
  resetError.value = ''
  resetOpen.value = true
}

async function confirmReset(): Promise<void> {
  if (!resetTarget.value || saving.value) return
  resetError.value = resetValue.value ? '' : 'Escribe la contraseña temporal.'
  if (resetError.value) return
  saving.value = true
  try {
    const saved = await resetPassword(resetTarget.value.id, resetValue.value)
    resetOpen.value = false
    toast.value?.show(`Contraseña de ${saved.fullName} restablecida. Deberá cambiarla al entrar.`)
    await load()
  } catch (cause) {
    resetError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Usuarios" description="Cuentas para entrar al panel y sus roles en ganadería y quesería.">
    <template #actions>
      <div class="panel-toolbar"><UiButton @click="open(null)">Nuevo usuario</UiButton></div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!users" aria-busy="true">Cargando…</p>
    <UiCard v-else :title="`${users.length} cuenta${users.length === 1 ? '' : 's'}`">
      <UiTable label="Usuarios" :columns="columns" :rows="users">
        <template #cell-fullName="{ row }">
          <strong>{{ row.fullName }}</strong><span v-if="row.id === session?.id" class="panel-muted"> · tú</span>
        </template>
        <template #cell-roles="{ row }">{{ row.roles.map((role) => role.name).join(', ') }}</template>
        <template #cell-active="{ row }">
          <UiBadge v-if="!row.active" tone="danger">Desactivada</UiBadge>
          <UiBadge v-else-if="row.lockedUntil" tone="warning">Bloqueada por intentos</UiBadge>
          <UiBadge v-else-if="row.mustChangePassword" tone="info">Contraseña temporal</UiBadge>
          <UiBadge v-else tone="success">Activa</UiBadge>
        </template>
        <template #cell-lastLoginAt="{ row }">{{ dateTime(row.lastLoginAt) }}</template>
        <template #cell-id="{ row }">
          <div class="us-actions">
            <button type="button" class="panel-icon-button panel-icon-button--neutral" :aria-label="`Editar a ${row.fullName}`" @click="open(row)">Editar</button>
            <button type="button" class="panel-icon-button panel-icon-button--neutral" :aria-label="`Restablecer contraseña de ${row.fullName}`" @click="openReset(row)">Contraseña</button>
          </div>
        </template>
      </UiTable>
    </UiCard>

    <UiDialog v-model="dialogOpen" :title="editing ? `Editar a ${editing.fullName}` : 'Nuevo usuario'">
      <form id="us-form" class="ui-form" novalidate @submit.prevent="save">
        <UiField v-model="form.fullName" label="Nombre completo *" maxlength="120" autocomplete="off" />
        <UiField v-if="!editing" v-model="form.username" label="Usuario *" maxlength="32" autocomplete="off" autocapitalize="none" spellcheck="false" hint="Minúsculas, números, punto o guion. Ej. pedro.ramos" />
        <UiField v-if="!editing" v-model="form.password" label="Contraseña temporal *" type="text" maxlength="128" autocomplete="off" hint="8 o más, con letra y número. La cambiará al entrar." />
        <UiField v-model="form.email" label="Correo" type="email" maxlength="254" autocomplete="off" />
        <fieldset v-for="division in rolesByDivision" :key="division.key" class="us-roles">
          <legend>{{ division.label }}</legend>
          <label v-for="role in division.roles" :key="role.code" class="us-role">
            <input type="checkbox" :checked="form.roleCodes.includes(role.code)" @change="toggleRole(role.code, ($event.target as HTMLInputElement).checked)" />
            <span><strong>{{ role.name }}</strong><span class="panel-muted"> · {{ role.description }}</span></span>
          </label>
        </fieldset>
        <UiToggle v-if="editing" v-model="form.active" label="Cuenta activa" />
        <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="us-form" :loading="saving">{{ editing ? 'Guardar cambios' : 'Crear usuario' }}</UiButton>
        <UiButton variant="secondary" @click="dialogOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiDialog v-model="resetOpen" :title="`Restablecer contraseña de ${resetTarget?.fullName ?? ''}`">
      <form id="us-reset-form" class="ui-form" novalidate @submit.prevent="confirmReset">
        <p>También desbloquea la cuenta. La persona deberá cambiarla al entrar.</p>
        <UiField v-model="resetValue" label="Contraseña temporal *" type="text" maxlength="128" autocomplete="off" />
        <UiAlert v-if="resetError" tone="danger" title="No se restableció">{{ resetError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="us-reset-form" :loading="saving">Restablecer</UiButton>
        <UiButton variant="secondary" @click="resetOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.us-actions { display: flex; flex-wrap: wrap; gap: 4px 16px; }
.us-actions .panel-icon-button { padding: 0; }
.us-roles { border: 1px solid var(--color-border); border-radius: 8px; padding: 8px 12px; margin: 0 0 12px; min-width: 0; }
.us-roles legend { padding: 0 6px; font-weight: 600; }
.us-role { display: flex; align-items: flex-start; gap: 10px; min-height: 44px; padding: 6px 0; cursor: pointer; }
.us-role input { width: 18px; height: 18px; margin-top: 3px; flex: none; accent-color: var(--color-primary); }
</style>
