<script setup lang="ts">
import { RouterView } from 'vue-router'
import { UiHeader, UiShell, UiSidebar, UiToggle } from '../../shared/components'
import { useTheme } from '../../shared/composables/useTheme'
import { headerNavigation, mainLayoutConfig, sidebarNavigation } from './mainLayout.config'

const { dark } = useTheme()
</script>

<template>
  <UiShell :show-header="mainLayoutConfig.showHeader" :show-sidebar="mainLayoutConfig.showSidebar">
    <template #header>
      <UiHeader v-bind="mainLayoutConfig.header" :items="headerNavigation">
        <UiToggle v-model="dark" type="switch" label="Modo oscuro" class="theme" />
      </UiHeader>
    </template>
    <template #sidebar>
      <UiSidebar v-bind="mainLayoutConfig.sidebar" :items="sidebarNavigation" note="Bovinos para carne. Caprinos para cabritos y leche. Quesería artesanal.">
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
