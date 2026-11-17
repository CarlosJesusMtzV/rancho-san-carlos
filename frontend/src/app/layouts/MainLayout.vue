<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'

import { logout, session } from '../../shared/auth/session'
import { UiButton, UiHeader, UiShell, UiSidebar, UiToggle } from '../../shared/components'
import type { NavigationItem } from '../../shared/components/navigation'
import { useTheme } from '../../shared/composables/useTheme'
import { canVisit } from '../access'
import { headerNavigation, mainLayoutConfig, sidebarNavigation } from './mainLayout.config'

const { dark } = useTheme()
const router = useRouter()

// El menú solo muestra lo que el rol permite abrir; un grupo sin enlaces desaparece.
function allowed(items: NavigationItem[]): NavigationItem[] {
  return items.flatMap<NavigationItem>((item) => {
    if ('children' in item) {
      const children = item.children.filter((child) => typeof child.to === 'string' && canVisit(child.to))
      return children.length ? [{ ...item, children }] : []
    }
    return typeof item.to === 'string' && canVisit(item.to) ? [item] : []
  })
}
const sidebarItems = computed(() => allowed(sidebarNavigation))
const headerItems = computed(() => allowed(headerNavigation))
const roleNames = computed(() => session.value?.roles.map((role) => role.name).join(', ') ?? '')

async function signOut(): Promise<void> {
  await logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <UiShell :show-header="mainLayoutConfig.showHeader" :show-sidebar="mainLayoutConfig.showSidebar">
    <template #header>
      <UiHeader v-bind="mainLayoutConfig.header" :items="headerItems">
        <UiToggle v-model="dark" type="switch" label="Modo oscuro" class="theme" />
      </UiHeader>
    </template>
    <template #sidebar>
      <UiSidebar v-bind="mainLayoutConfig.sidebar" :items="sidebarItems">
        <div v-if="session" class="session-box">
          <p class="session-name"><strong>{{ session.fullName }}</strong></p>
          <p class="session-roles">{{ roleNames }}</p>
          <RouterLink class="panel-link" :to="{ name: 'account' }">Mi cuenta</RouterLink>
          <UiButton variant="secondary" @click="signOut">Cerrar sesión</UiButton>
        </div>
        <UiToggle v-if="!mainLayoutConfig.showHeader" v-model="dark" type="switch" label="Modo oscuro" />
      </UiSidebar>
    </template>
    <RouterView />
    <template #footer>
      <span>Rancho San Carlos y La Cremería San Carlos · Castaños, Coahuila</span>
      <span>Proyecto académico TecNM Campus Monclova</span>
    </template>
  </UiShell>
</template>

<style scoped>
.session-box { display: grid; gap: 6px; justify-items: start; padding: 12px 0; margin-top: 12px; border-top: 1px solid var(--color-border-light); min-width: 0; }
.session-name, .session-roles { margin: 0; overflow-wrap: anywhere; }
.session-roles { font-size: 14px; opacity: 0.8; }
</style>
