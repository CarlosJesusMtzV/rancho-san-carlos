<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiRadio } from '../../../shared/components'
import { addDays, formatDate, todayIso } from '../../../shared/utils/format'
import { listAnimals, type AnimalSummary } from '../../herd'
import { createBreeding, listFemales } from '../api/reproductionApi'
import { gestationDays } from '../data/labels'
import type { FemaleStatus } from '../types/reproduction'

const route = useRoute()
const router = useRouter()
const today = todayIso()

const openFemales = ref<FemaleStatus[]>([])
const males = ref<AnimalSummary[]>([])
const loadError = ref('')
const ready = ref(false)
const saveError = ref('')
const saving = ref(false)

const form = reactive({ species: '1' as '1' | '2', femaleId: '', maleId: '', matingDate: today, notes: '' })
const speciesId = computed(() => Number(form.species) as 1 | 2)

const femaleOptions = computed(() => [
  { value: '', label: 'Selecciona la hembra' },
  ...openFemales.value
    .filter((female) => female.speciesId === speciesId.value)
    .map((female) => ({ value: String(female.animalId), label: `${female.visualTag} · ${female.breedName}` })),
])
const maleOptions = computed(() => [
  { value: '', label: 'Selecciona el semental' },
  ...males.value
    .filter((male) => male.speciesId === speciesId.value)
    .map((male) => ({ value: String(male.id), label: `${male.visualTag} · ${male.breedName}` })),
])
const expectedDue = computed(() => (form.matingDate ? addDays(form.matingDate, gestationDays(speciesId.value)) : ''))

watch(speciesId, () => {
  form.femaleId = ''
  form.maleId = ''
})

async function load(): Promise<void> {
  loadError.value = ''
  try {
    const [females, malePage] = await Promise.all([listFemales({ status: 'open' }), listAnimals({ sex: 'M', status: 'active', pageSize: 200 })])
    openFemales.value = females
    males.value = malePage.items
    const preselected = females.find((female) => female.animalId === Number(route.query.hembra))
    if (preselected) {
      form.species = String(preselected.speciesId) as '1' | '2'
      await nextTick() // deja que el cambio de especie limpie el formulario antes de elegir la hembra
      form.femaleId = String(preselected.animalId)
    }
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  if (!form.femaleId || !form.maleId || !form.matingDate) {
    saveError.value = 'Elige hembra, semental y fecha.'
    return
  }
  saving.value = true
  try {
    const breeding = await createBreeding({
      femaleId: Number(form.femaleId),
      maleId: Number(form.maleId),
      matingDate: form.matingDate,
      notes: form.notes.trim() || null,
    })
    await router.push({
      name: 'reproduction',
      query: { guardado: `Monta de ${breeding.femaleTag} registrada. Parto probable: ${formatDate(breeding.expectedDueDate)}.` },
    })
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Registrar monta" description="Monta natural. Solo aparecen hembras vacías y machos activos de la especie elegida.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Volver a reproducción</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se registró">{{ saveError }}</UiAlert>
      <UiCard title="Monta">
        <div class="panel-form">
          <fieldset class="panel-radios panel-span">
            <legend>Especie *</legend>
            <UiRadio v-model="form.species" name="species" value="1" label="Vacas" />
            <UiRadio v-model="form.species" name="species" value="2" label="Chivas" />
          </fieldset>
          <UiField v-model="form.femaleId" as="select" label="Hembra *" :options="femaleOptions" />
          <UiField v-model="form.maleId" as="select" label="Semental *" :options="maleOptions" />
          <UiField v-model="form.matingDate" label="Fecha de monta *" type="date" :max="today" required />
          <UiField v-model="form.notes" label="Notas" maxlength="255" />
        </div>
        <p v-if="femaleOptions.length === 1" class="panel-muted">No hay hembras vacías de esta especie.</p>
        <p v-if="maleOptions.length === 1" class="panel-muted">No hay machos activos de esta especie.</p>
      </UiCard>

      <UiAlert v-if="expectedDue" tone="info" title="Parto probable">
        {{ formatDate(expectedDue) }} ({{ gestationDays(speciesId) }} días de gestación).
      </UiAlert>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">Registrar monta</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Cancelar</RouterLink>
      </div>
    </form>
  </UiBasePage>
</template>
