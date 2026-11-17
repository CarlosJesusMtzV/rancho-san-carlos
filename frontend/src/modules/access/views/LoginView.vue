<script setup lang="ts">
import { reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { login } from '../../../shared/auth/session'
import { UiAlert, UiBrand, UiButton, UiCard, UiField } from '../../../shared/components'

const route = useRoute()
const router = useRouter()
const form = reactive({ username: '', password: '' })
const error = ref('')
const loading = ref(false)

async function submit(): Promise<void> {
  if (loading.value) return
  error.value = form.username.trim() && form.password ? '' : 'Escribe tu usuario y tu contraseña.'
  if (error.value) return
  loading.value = true
  try {
    await login(form.username.trim(), form.password)
    // Solo se regresa a rutas internas del panel.
    const redirect = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/panel') ? route.query.redirect : '/panel'
    await router.replace(redirect)
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
    form.password = ''
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="login-page">
    <UiCard class="login-card">
      <UiBrand title="San Carlos" subtitle="Ganadería y quesería" show-title show-logo />
      <h1 class="login-title">Entrar al panel</h1>
      <UiAlert v-if="route.query.vencida && !error" tone="info" title="Tu sesión terminó">Vuelve a entrar para continuar.</UiAlert>
      <form class="ui-form" novalidate @submit.prevent="submit">
        <UiField v-model="form.username" label="Usuario" autocomplete="username" autocapitalize="none" spellcheck="false" required />
        <UiField v-model="form.password" label="Contraseña" type="password" autocomplete="current-password" required />
        <UiAlert v-if="error" tone="danger" title="No se pudo entrar">{{ error }}</UiAlert>
        <UiButton type="submit" :loading="loading">Entrar</UiButton>
      </form>
      <p class="panel-muted">Si olvidaste tu contraseña, pídele al dueño que la restablezca.</p>
      <RouterLink class="panel-link" to="/">Volver al sitio público</RouterLink>
    </UiCard>
  </main>
</template>

<style scoped>
.login-page { min-height: 100vh; display: grid; place-items: center; padding: 24px 16px; background: var(--color-background); }
.login-card { width: min(100%, 420px); display: grid; gap: 16px; }
.login-title { margin: 0; font-size: 22px; }
.login-card .ui-form { display: grid; gap: 4px; }
</style>
