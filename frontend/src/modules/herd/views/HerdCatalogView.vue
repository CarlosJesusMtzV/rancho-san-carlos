<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiTable, UiToast } from '../../../shared/components'
import { createBreed, createLot } from '../api/herdApi'
import LoadError from '../components/LoadError.vue'
import SpeciesTag from '../components/SpeciesTag.vue'
import { useCatalog } from '../composables/useCatalog'
import type { SpeciesId } from '../types/herd'
import '../styles.css'

const { catalog, loading, error, load } = useCatalog()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const speciesOptions = [
  { value: '1', label: 'Bovino' },
  { value: '2', label: 'Caprino' },
]

const breedForm = reactive({ speciesId: '1', name: '', purpose: '' })
const breedError = ref('')
const savingBreed = ref(false)

const lotForm = reactive({ speciesId: '1', code: '', description: '' })
const lotError = ref('')
const savingLot = ref(false)

const breedColumns = [
  { key: 'name' as const, label: 'Raza' },
  { key: 'speciesId' as const, label: 'Especie' },
  { key: 'purpose' as const, label: 'Propósito' },
]
const lotColumns = [
  { key: 'code' as const, label: 'Lote' },
  { key: 'speciesId' as const, label: 'Especie' },
  { key: 'description' as const, label: 'Descripción' },
  { key: 'activeAnimals' as const, label: 'Animales activos' },
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
  savingBreed.value = true
  try {
    const breed = await createBreed({
      speciesId: Number(breedForm.speciesId) as SpeciesId,
      name: breedForm.name.trim(),
      purpose: breedForm.purpose.trim() || null,
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

async function saveLot(): Promise<void> {
  if (savingLot.value) return
  lotError.value = ''
  if (!lotForm.code.trim()) {
    lotError.value = 'Escribe el código del lote.'
    return
  }
  savingLot.value = true
  try {
    const lot = await createLot({
      speciesId: Number(lotForm.speciesId) as SpeciesId,
      code: lotForm.code.trim(),
      description: lotForm.description.trim() || null,
    })
    lotForm.code = ''
    lotForm.description = ''
    await load()
    toast.value?.show(`Lote ${lot.code} registrado.`)
  } catch (cause) {
    lotError.value = getApiErrorMessage(cause)
  } finally {
    savingLot.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Razas y lotes" description="Catálogos del hato. Cada raza y cada lote pertenecen a una sola especie.">
    <template #actions>
      <div class="herd-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'herd' }">Volver al hato</RouterLink>
      </div>
    </template>

    <LoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="loading && !catalog" aria-busy="true">Cargando…</p>

    <div v-else class="grid">
      <UiCard title="Razas">
        <form class="herd-form" novalidate @submit.prevent="saveBreed">
          <UiField v-model="breedForm.speciesId" as="select" label="Especie *" :options="speciesOptions" />
          <UiField v-model="breedForm.name" label="Nombre *" maxlength="80" placeholder="Ej. Charolais, Alpina" required />
          <UiField v-model="breedForm.purpose" label="Propósito" maxlength="120" placeholder="Ej. Carne, leche" />
          <div class="herd-span"><UiButton type="submit" :loading="savingBreed">Agregar raza</UiButton></div>
        </form>
        <UiAlert v-if="breedError" tone="danger" title="No se registró">{{ breedError }}</UiAlert>
        <p v-if="breeds.length === 0" class="herd-muted">Aún no hay razas.</p>
        <UiTable v-else label="Razas registradas" :columns="breedColumns" :rows="breeds">
          <template #cell-speciesId="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
          <template #cell-purpose="{ row }">{{ row.purpose ?? '—' }}</template>
        </UiTable>
      </UiCard>

      <UiCard title="Lotes">
        <form class="herd-form" novalidate @submit.prevent="saveLot">
          <UiField v-model="lotForm.speciesId" as="select" label="Especie *" :options="speciesOptions" />
          <UiField v-model="lotForm.code" label="Código *" maxlength="30" placeholder="Ej. BOV-DESTETE" hint="Se guarda en mayúsculas." required />
          <UiField v-model="lotForm.description" label="Descripción" maxlength="255" />
          <div class="herd-span"><UiButton type="submit" :loading="savingLot">Agregar lote</UiButton></div>
        </form>
        <UiAlert v-if="lotError" tone="danger" title="No se registró">{{ lotError }}</UiAlert>
        <p v-if="lots.length === 0" class="herd-muted">Aún no hay lotes.</p>
        <UiTable v-else label="Lotes registrados" :columns="lotColumns" :rows="lots">
          <template #cell-speciesId="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
          <template #cell-description="{ row }">{{ row.description ?? '—' }}</template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
