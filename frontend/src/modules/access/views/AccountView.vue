<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { changePassword, session } from '../../../shared/auth/session'
import { UiAlert, UiBadge, UiBasePage, UiButton, UiCard, UiField, UiToast } from '../../../shared/components'

const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const form = reactive({ current: '', next: '', repeat: '' })
const error = ref('')
const saving = ref(false)
const divisionLabels = { general: 'General', livestock: 'Ganadería', cheese: 'Quesería' }

async function save(): Promise<void> {
  if (saving.value) return
  error.value = !form.current || !form.next ? 'Escribe tu contraseña actual y la nueva.' : form.next !== form.repeat ? 'La contraseña nueva no coincide en los dos campos.' : ''
  if (error.value) return
  saving.value = true
  const wasTemporary = session.value?.mustChangePassword
  try {
    await changePassword(form.current, form.next)
    Object.assign(form, { current: '', next: '', repeat: '' })
    if (wasTemporary) await router.replace('/panel')
    else toast.value?.show('Contraseña cambiada.')
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <UiBasePage fluid title="Mi cuenta" :description="session ? `${session.fullName} · usuario ${session.username}` : undefined">
    <div class="stack">
      <UiAlert v-if="session?.mustChangePassword" tone="warning" title="Cambia tu contraseña temporal">
        Te la dio el dueño para tu primer acceso. Elige una que solo tú conozcas para poder usar el panel.
      </UiAlert>

      <UiCard v-if="session" title="Mis roles" description="Definen qué puedes ver y hacer. Los asigna el dueño.">
        <ul class="ac-roles">
          <li v-for="role in session.roles" :key="role.code">
            <strong>{{ role.name }}</strong> <UiBadge tone="info">{{ divisionLabels[role.division] }}</UiBadge>
            <p class="panel-muted">{{ role.description }}</p>
          </li>
        </ul>
      </UiCard>

      <UiCard title="Cambiar contraseña" description="De 8 a 128 caracteres, con al menos una letra y un número.">
        <form class="stack" novalidate @submit.prevent="save">
          <div class="panel-form">
            <UiField v-model="form.current" label="Contraseña actual *" type="password" autocomplete="current-password" />
            <UiField v-model="form.next" label="Contraseña nueva *" type="password" autocomplete="new-password" minlength="8" maxlength="128" />
            <UiField v-model="form.repeat" label="Repite la contraseña nueva *" type="password" autocomplete="new-password" maxlength="128" />
          </div>
          <UiAlert v-if="error" tone="danger" title="No se cambió">{{ error }}</UiAlert>
          <div class="panel-actions"><UiButton type="submit" :loading="saving">Cambiar contraseña</UiButton></div>
        </form>
      </UiCard>
    </div>
    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.ac-roles { list-style: none; margin: 0; padding: 0; display: grid; gap: 12px; }
.ac-roles p { margin: 4px 0 0; }
</style>
