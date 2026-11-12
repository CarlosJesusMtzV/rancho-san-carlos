<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiChip, UiField, UiLoadError, UiRadio } from '../../../shared/components'
import { formatDate, todayIso } from '../../../shared/utils/format'
import { getHerdCatalog, listAnimals, speciesLabels, type AnimalSummary, type LotItem, type SpeciesId } from '../../herd'
import { createEvents, getHealthCatalog, listVisits } from '../api/healthApi'
import { eventTypeLabels, eventTypeOptions, typesRequiringTreatment } from '../data/labels'
import type { HealthEventType, MedicationItem, VetVisitSummary } from '../types/health'
import '../styles.css'

const route = useRoute()
const router = useRouter()
const today = todayIso()

const loadError = ref('')
const ready = ref(false)
const animals = ref<AnimalSummary[]>([])
const lots = ref<LotItem[]>([])
const medications = ref<MedicationItem[]>([])
const visits = ref<VetVisitSummary[]>([])

interface TreatmentRow {
  key: number
  supplyId: string
  dose: string
  milkDays: string
  meatDays: string
}

let nextKey = 1
const form = reactive({
  target: 'animals' as 'animals' | 'lot',
  speciesFilter: 0 as 0 | SpeciesId,
  search: '',
  selected: [] as number[],
  lotId: '',
  eventDate: today,
  eventType: 'vaccination' as HealthEventType,
  diagnosis: '',
  visitId: '',
  treatments: [] as TreatmentRow[],
})
const saveError = ref('')
const saving = ref(false)

const selectedVisit = computed(() => visits.value.find((visit) => String(visit.id) === form.visitId) ?? null)
const visibleAnimals = computed(() => {
  const term = form.search.trim().toUpperCase()
  return animals.value.filter(
    (animal) =>
      (form.speciesFilter === 0 || animal.speciesId === form.speciesFilter) &&
      (!term || animal.visualTag.includes(term) || (animal.officialTag ?? '').includes(term)),
  )
})
const selectedTags = computed(() =>
  animals.value.filter((animal) => form.selected.includes(animal.id)).map((animal) => animal.visualTag),
)
const lotOptions = computed(() => [
  { value: '', label: 'Selecciona un lote' },
  ...lots.value.map((lot) => ({
    value: String(lot.id),
    label: `${lot.code} · ${speciesLabels[lot.speciesId]} · ${lot.activeAnimals} activo${lot.activeAnimals === 1 ? '' : 's'}`,
  })),
])
const visitOptions = computed(() => [
  { value: '', label: 'Sin visita (lo aplicó la familia)' },
  ...visits.value.map((visit) => ({ value: String(visit.id), label: `${formatDate(visit.visitDate)} · ${visit.vetName}` })),
])
const medicationOptions = computed(() => [
  { value: '', label: 'Selecciona' },
  ...medications.value.map((medication) => ({ value: String(medication.id), label: `${medication.commercialName} (${medication.supplyTypeName})` })),
])
const needsTreatment = computed(() => typesRequiringTreatment.includes(form.eventType))
const targetCount = computed(() =>
  form.target === 'lot' ? (lots.value.find((lot) => String(lot.id) === form.lotId)?.activeAnimals ?? 0) : form.selected.length,
)

// La fecha del evento es la de la visita (regla S4).
watch(selectedVisit, (visit) => {
  if (visit) form.eventDate = visit.visitDate
})

function addTreatment(): void {
  form.treatments.push({ key: nextKey++, supplyId: '', dose: '', milkDays: '0', meatDays: '0' })
}

function removeTreatment(key: number): void {
  form.treatments = form.treatments.filter((row) => row.key !== key)
}

// Al elegir un medicamento se proponen sus retiros; quedan editables.
function chooseMedication(row: TreatmentRow, supplyId: string): void {
  row.supplyId = supplyId
  const medication = medications.value.find((item) => String(item.id) === supplyId)
  if (medication) {
    row.milkDays = String(medication.defaultMilkWithdrawalDays)
    row.meatDays = String(medication.defaultMeatWithdrawalDays)
  }
}

function toggleAnimal(id: number, checked: boolean): void {
  form.selected = checked ? [...form.selected, id] : form.selected.filter((item) => item !== id)
}

async function load(): Promise<void> {
  loadError.value = ''
  ready.value = false
  try {
    const [animalPage, herdCatalog, healthCatalog, visitList] = await Promise.all([
      listAnimals({ status: 'active', pageSize: 200 }),
      getHerdCatalog(),
      getHealthCatalog(),
      listVisits(),
    ])
    animals.value = animalPage.items
    lots.value = herdCatalog.lots.filter((lot) => lot.activeAnimals > 0)
    medications.value = healthCatalog.medications
    visits.value = visitList

    const visitId = Number(route.query.visita)
    if (visitList.some((visit) => visit.id === visitId)) form.visitId = String(visitId)
    const animalId = Number(route.query.animal)
    if (animalPage.items.some((animal) => animal.id === animalId)) form.selected = [animalId]
    if (form.treatments.length === 0) addTreatment()
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

function validate(): string {
  if (form.target === 'animals' && form.selected.length === 0) return 'Elige al menos un animal.'
  if (form.target === 'lot' && !form.lotId) return 'Elige el lote.'
  if (!form.eventDate) return 'Indica la fecha del evento.'
  const filled = form.treatments.filter((row) => row.supplyId)
  if (needsTreatment.value && filled.length === 0) {
    return `Una ${eventTypeLabels[form.eventType].toLowerCase()} necesita al menos un medicamento.`
  }
  if (filled.some((row) => !row.dose.trim())) return 'Escribe la dosis de cada medicamento.'
  const days = filled.flatMap((row) => [Number(row.milkDays), Number(row.meatDays)])
  if (days.some((value) => !Number.isInteger(value) || value < 0 || value > 365)) return 'Los días de retiro van de 0 a 365.'
  return ''
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = validate()
  if (saveError.value) return

  saving.value = true
  try {
    const result = await createEvents({
      ...(form.target === 'lot' ? { lotId: Number(form.lotId) } : { animalIds: form.selected }),
      eventDate: form.eventDate,
      eventType: form.eventType,
      diagnosis: form.diagnosis.trim() || null,
      vetVisitId: form.visitId ? Number(form.visitId) : null,
      treatments: form.treatments
        .filter((row) => row.supplyId)
        .map((row) => ({
          supplyId: Number(row.supplyId),
          dose: row.dose.trim(),
          milkWithdrawalDays: Number(row.milkDays),
          meatWithdrawalDays: Number(row.meatDays),
        })),
    })
    // La vista de destino muestra la confirmación a partir de ?registrados=.
    const registrados = String(result.created)
    await router.push(
      form.visitId
        ? { name: 'health-visit', params: { id: form.visitId }, query: { registrados } }
        : {
            name: 'health-history',
            query: form.target === 'animals' && form.selected.length === 1 ? { animal: form.selected[0], registrados } : { registrados },
          },
    )
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Registrar evento sanitario" description="Para uno o varios animales, o para todos los activos de un lote.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'health' }">Volver a sanidad</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se registró">{{ saveError }}</UiAlert>

      <UiCard title="¿A quién se aplicó?">
        <fieldset class="panel-radios">
          <legend>Destino *</legend>
          <UiRadio v-model="form.target" name="target" value="animals" label="Animales elegidos" />
          <UiRadio v-model="form.target" name="target" value="lot" label="Todo un lote" />
        </fieldset>

        <template v-if="form.target === 'animals'">
          <div class="panel-chips" role="group" aria-label="Especie">
            <UiChip :pressed="form.speciesFilter === 0" @click="form.speciesFilter = 0">Todas</UiChip>
            <UiChip :pressed="form.speciesFilter === 1" @click="form.speciesFilter = 1">Bovinos</UiChip>
            <UiChip :pressed="form.speciesFilter === 2" @click="form.speciesFilter = 2">Caprinos</UiChip>
          </div>
          <UiField v-model="form.search" label="Buscar arete" type="search" autocomplete="off" placeholder="Ej. SC-CAB" />
          <p v-if="animals.length === 0" class="panel-muted">
            No hay animales activos. <RouterLink class="panel-link" to="/panel/hato/nuevo">Registrar animal</RouterLink>
          </p>
          <div v-else class="hl-animal-list" role="group" aria-label="Animales activos">
            <label v-for="animal in visibleAnimals" :key="animal.id" class="hl-animal">
              <input type="checkbox" :checked="form.selected.includes(animal.id)" @change="toggleAnimal(animal.id, ($event.target as HTMLInputElement).checked)" />
              <span><strong>{{ animal.visualTag }}</strong> <span class="panel-muted">· {{ speciesLabels[animal.speciesId] }} · {{ animal.breedName }}</span></span>
            </label>
            <p v-if="visibleAnimals.length === 0" class="panel-muted">Ningún animal coincide.</p>
          </div>
          <p class="panel-muted" aria-live="polite">
            {{ form.selected.length }} seleccionado{{ form.selected.length === 1 ? '' : 's' }}<template v-if="selectedTags.length">: {{ selectedTags.slice(0, 6).join(', ') }}{{ selectedTags.length > 6 ? '…' : '' }}</template>
          </p>
        </template>

        <UiField v-else v-model="form.lotId" as="select" label="Lote *" :options="lotOptions" hint="Se registra para cada animal activo del lote." />
      </UiCard>

      <UiCard title="Evento">
        <div class="panel-form">
          <UiField v-model="form.visitId" as="select" label="Visita del veterinario" :options="visitOptions" />
          <UiField
            v-model="form.eventDate"
            label="Fecha *"
            type="date"
            :max="today"
            :disabled="!!selectedVisit"
            :hint="selectedVisit ? 'Es la fecha de la visita.' : undefined"
          />
          <UiField v-model="form.eventType" as="select" label="Tipo *" :options="eventTypeOptions" />
          <UiField v-model="form.diagnosis" label="Diagnóstico o motivo" maxlength="255" />
        </div>
      </UiCard>

      <UiCard title="Medicamentos" :description="needsTreatment ? 'Obligatorio para este tipo de evento.' : 'Opcional.'">
        <p v-if="medications.length === 0" class="panel-muted">
          No hay medicamentos en el catálogo.
          <RouterLink class="panel-link" :to="{ name: 'health-medications' }">Registrar medicamentos</RouterLink>
        </p>
        <template v-else>
          <div v-for="(row, index) in form.treatments" :key="row.key" class="hl-treatment">
            <UiField
              as="select"
              :label="`Medicamento ${index + 1}`"
              :options="medicationOptions"
              :model-value="row.supplyId"
              @update:model-value="chooseMedication(row, $event)"
            />
            <UiField v-model="row.dose" label="Dosis" maxlength="60" placeholder="Ej. 5 ml IM" />
            <UiField v-model="row.milkDays" label="Retiro leche (días)" type="number" min="0" max="365" step="1" inputmode="numeric" />
            <UiField v-model="row.meatDays" label="Retiro carne (días)" type="number" min="0" max="365" step="1" inputmode="numeric" />
            <button type="button" class="panel-icon-button" :aria-label="`Quitar medicamento ${index + 1}`" @click="removeTreatment(row.key)">Quitar</button>
          </div>
          <UiButton variant="secondary" :disabled="form.treatments.length >= 10" @click="addTreatment">Agregar medicamento</UiButton>
        </template>
      </UiCard>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">
          Registrar{{ targetCount > 1 ? ` para ${targetCount} animales` : '' }}
        </UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'health' }">Cancelar</RouterLink>
      </div>
    </form>
  </UiBasePage>
</template>
