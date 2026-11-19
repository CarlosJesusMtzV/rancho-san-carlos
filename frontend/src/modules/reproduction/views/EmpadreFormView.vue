<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiRadio } from '../../../shared/components'
import { addDays, formatDate, todayIso } from '../../../shared/utils/format'
import { listAnimals, type AnimalSummary } from '../../herd'
import { createEmpadre, listFemales } from '../api/reproductionApi'
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

const form = reactive({ species: '1' as '1' | '2', maleId: '', startDate: today, notes: '' })
const selected = ref<Set<number>>(new Set())
const speciesId = computed(() => Number(form.species) as 1 | 2)

const femalesHere = computed(() => openFemales.value.filter((female) => female.speciesId === speciesId.value))
const maleOptions = computed(() => [
  { value: '', label: 'Selecciona el semental' },
  ...males.value
    .filter((male) => male.speciesId === speciesId.value)
    .map((male) => ({ value: String(male.id), label: `${male.visualTag} · ${male.breedName}` })),
])
const selectedCount = computed(() => femalesHere.value.filter((female) => selected.value.has(female.animalId)).length)
const allSelected = computed(() => femalesHere.value.length > 0 && selectedCount.value === femalesHere.value.length)
const expectedDue = computed(() => (form.startDate ? addDays(form.startDate, gestationDays(speciesId.value)) : ''))

function toggle(id: number): void {
  const next = new Set(selected.value)
  next.has(id) ? next.delete(id) : next.add(id)
  selected.value = next
}

function toggleAll(): void {
  selected.value = allSelected.value ? new Set() : new Set(femalesHere.value.map((female) => female.animalId))
}

watch(speciesId, () => {
  form.maleId = ''
  selected.value = new Set()
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
      await nextTick() // deja que el cambio de especie limpie la selección antes de marcar la hembra
      selected.value = new Set([preselected.animalId])
    }
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  const femaleIds = femalesHere.value.filter((female) => selected.value.has(female.animalId)).map((female) => female.animalId)
  if (!form.maleId || femaleIds.length === 0 || !form.startDate) {
    saveError.value = 'Elige el semental, al menos una hembra y la fecha de entrada.'
    return
  }
  saving.value = true
  try {
    const empadre = await createEmpadre({
      maleId: Number(form.maleId),
      femaleIds,
      startDate: form.startDate,
      notes: form.notes.trim() || null,
    })
    await router.push({
      name: 'reproduction',
      query: { guardado: `Empadre de ${empadre.females.length} hembra(s) con ${empadre.maleTag} registrado. Parto probable: ${formatDate(empadre.females[0].expectedDueDate)}.` },
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
  <UiBasePage fluid title="Registrar empadre" description="Se junta un semental con las hembras elegidas. Se anota cuándo entró el semental; el parto probable es estimado.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Volver a reproducción</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se registró">{{ saveError }}</UiAlert>
      <UiCard title="Empadre">
        <div class="panel-form">
          <fieldset class="panel-radios panel-span">
            <legend>Especie *</legend>
            <UiRadio v-model="form.species" name="species" value="1" label="Vacas" />
            <UiRadio v-model="form.species" name="species" value="2" label="Chivas" />
          </fieldset>
          <UiField v-model="form.maleId" as="select" label="Semental *" :options="maleOptions" />
          <UiField v-model="form.startDate" label="Entró el semental *" type="date" :max="today" required />
          <UiField v-model="form.notes" label="Notas" maxlength="255" />
        </div>
        <p v-if="maleOptions.length === 1" class="panel-muted">No hay machos activos de esta especie.</p>
      </UiCard>

      <UiCard :title="`Hembras del empadre (${selectedCount})`">
        <p v-if="femalesHere.length === 0" class="panel-muted">No hay hembras vacías de esta especie.</p>
        <template v-else>
          <label class="empadre-female empadre-all">
            <input type="checkbox" :checked="allSelected" @change="toggleAll" />
            <span>Seleccionar todas</span>
          </label>
          <ul class="empadre-list">
            <li v-for="female in femalesHere" :key="female.animalId">
              <label class="empadre-female">
                <input type="checkbox" :checked="selected.has(female.animalId)" @change="toggle(female.animalId)" />
                <span>{{ female.visualTag }} · {{ female.breedName }}<span v-if="female.situation" class="panel-muted"> · {{ female.situation }}</span></span>
              </label>
            </li>
          </ul>
        </template>
      </UiCard>

      <UiAlert v-if="expectedDue && selectedCount > 0" tone="info" title="Parto probable">
        {{ formatDate(expectedDue) }} ({{ gestationDays(speciesId) }} días de gestación), estimado desde la entrada del semental.
      </UiAlert>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">Registrar empadre</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Cancelar</RouterLink>
      </div>
    </form>
  </UiBasePage>
</template>

<style scoped>
.empadre-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 0.25rem;
}
.empadre-female {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.4rem 0.25rem;
  cursor: pointer;
}
.empadre-all {
  border-bottom: 1px solid var(--color-border);
  margin-bottom: 0.25rem;
  font-weight: 600;
}
</style>
