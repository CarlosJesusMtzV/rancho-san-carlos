<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { can } from '../../../shared/auth/session'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import { createBreed } from '../api/herdApi'
import SpeciesTag from '../components/SpeciesTag.vue'
import { useCatalog } from '../composables/useCatalog'
import type { SpeciesId } from '../types/herd'

const { catalog, loading, error, load } = useCatalog()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const speciesOptions = [
  { value: '1', label: 'Bovino' },
  { value: '2', label: 'Caprino' },
]

const purposeOptions = [
  { value: '', label: 'Elige el propósito' },
  { value: 'meat', label: 'Carne' },
  { value: 'milk', label: 'Leche' },
  { value: 'meat_milk', label: 'Carne-leche' },
]
const purposeLabels: Record<string, string> = { meat: 'Carne', milk: 'Leche', meat_milk: 'Carne-leche' }

const breedForm = reactive({ speciesId: '1', name: '', purpose: '' })
const breedError = ref('')
const savingBreed = ref(false)

const breedColumns = [
  { key: 'name' as const, label: 'Raza' },
  { key: 'speciesId' as const, label: 'Especie' },
  { key: 'purpose' as const, label: 'Propósito' },
]
const lotColumns = [
  { key: 'description' as const, label: 'Situación' },
  { key: 'speciesId' as const, label: 'Especie' },
  { key: 'activeAnimals' as const, label: 'Animales' },
]

const breeds = computed(() => catalog.value?.breeds ?? [])
const lots = computed(() => catalog.value?.lots ?? [])

async function saveBreed(): Promise<void> {
  if (savingBreed.value) return
  breedError.value = ''
  if (!breedForm.name.trim()) {
    breedError.value = 'Escribe el nombre de la raza.'
    return
  }
  if (!breedForm.purpose) {
    breedError.value = 'Elige el propósito: carne, leche o carne-leche.'
    return
  }
  savingBreed.value = true
  try {
    const breed = await createBreed({
      speciesId: Number(breedForm.speciesId) as SpeciesId,
      name: breedForm.name.trim(),
      purpose: breedForm.purpose,
    })
    breedForm.name = ''
    breedForm.purpose = ''
    await load()
    toast.value?.show(`Raza ${breed.name} registrada.`)
  } catch (cause) {
    breedError.value = getApiErrorMessage(cause)
  } finally {
    savingBreed.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Razas y situaciones" description="Catálogos del hato. Las situaciones son fijas; las razas se agregan con un propósito (carne, leche o carne-leche).">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'herd' }">Volver al hato</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="loading && !catalog" aria-busy="true">Cargando…</p>

    <div v-else class="grid">
      <UiCard title="Razas">
        <form v-if="can('herd.write')" class="panel-form" novalidate @submit.prevent="saveBreed">
          <UiField v-model="breedForm.speciesId" as="select" label="Especie *" :options="speciesOptions" />
          <UiField v-model="breedForm.name" label="Nombre *" maxlength="80" placeholder="Ej. Limousin, Saanen" required />
          <UiField v-model="breedForm.purpose" as="select" label="Propósito *" :options="purposeOptions" />
          <div class="panel-span"><UiButton type="submit" :loading="savingBreed">Agregar raza</UiButton></div>
        </form>
        <UiAlert v-if="breedError" tone="danger" title="No se registró">{{ breedError }}</UiAlert>
        <p v-if="breeds.length === 0" class="panel-muted">Aún no hay razas.</p>
        <UiTable v-else label="Razas registradas" :columns="breedColumns" :rows="breeds">
          <template #cell-speciesId="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
          <template #cell-purpose="{ row }">{{ row.purpose ? purposeLabels[row.purpose] ?? row.purpose : '—' }}</template>
        </UiTable>
      </UiCard>

      <UiCard title="Situaciones">
        <p class="panel-muted">Lista fija: cómo se maneja el animal ahora mismo. Se elige al dar de alta o editar un animal.</p>
        <p v-if="lots.length === 0" class="panel-muted">Aún no hay situaciones.</p>
        <UiTable v-else label="Situaciones" :columns="lotColumns" :rows="lots">
          <template #cell-speciesId="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
