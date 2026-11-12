<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBadge, UiBasePage, UiButton, UiCard, UiChip, UiDialog, UiField, UiLoadError, UiMetric, UiTable, UiToast } from '../../../shared/components'
import { formatDate } from '../../../shared/utils/format'
import { listVisits, type VetVisitSummary } from '../../animal-health'
import { SpeciesTag } from '../../herd'
import { addCheck, listFemales } from '../api/reproductionApi'
import { checkResultOptions, dueText, outcomeLabels, outcomeTone } from '../data/labels'
import type { CheckResult, FemaleFilter, FemaleStatus } from '../types/reproduction'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const females = ref<FemaleStatus[] | null>(null)
const error = ref('')
const species = ref<0 | 1 | 2>(0)
const filter = ref<FemaleFilter>('all')

const visits = ref<VetVisitSummary[]>([])
const checkOpen = ref(false)
const checkTarget = ref<FemaleStatus | null>(null)
const checkForm = reactive({ visitId: '', result: 'pregnant' as CheckResult, notes: '' })
const checkError = ref('')
const savingCheck = ref(false)

const filterOptions: { value: FemaleFilter; label: string }[] = [
  { value: 'all', label: 'Todas' },
  { value: 'due-soon', label: 'Parto en 30 días' },
  { value: 'pregnant', label: 'Cargadas' },
  { value: 'pending', label: 'Por diagnosticar' },
  { value: 'open', label: 'Vacías' },
]

const inSpecies = computed(() => (females.value ?? []).filter((female) => species.value === 0 || female.speciesId === species.value))
const isOpen = (female: FemaleStatus) => !female.breeding || !['pending', 'pregnant'].includes(female.breeding.outcome)
const rows = computed(() =>
  inSpecies.value
    .filter((female) => {
      const outcome = female.breeding?.outcome
      if (filter.value === 'open') return isOpen(female)
      if (filter.value === 'pending') return outcome === 'pending'
      if (filter.value === 'pregnant') return outcome === 'pregnant'
      if (filter.value === 'due-soon') return outcome === 'pregnant' && (female.daysToDue ?? 99) <= 30
      return true
    })
    .map((female) => ({ ...female, id: female.animalId })),
)
const counts = computed(() => ({
  pregnant: inSpecies.value.filter((female) => female.breeding?.outcome === 'pregnant').length,
  pending: inSpecies.value.filter((female) => female.breeding?.outcome === 'pending').length,
  dueSoon: inSpecies.value.filter((female) => female.breeding?.outcome === 'pregnant' && (female.daysToDue ?? 99) <= 30).length,
  open: inSpecies.value.filter(isOpen).length,
}))

// Visitas posteriores a la monta: las únicas que el backend acepta para diagnosticar.
const visitOptions = computed(() => {
  const matingDate = checkTarget.value?.breeding?.matingDate ?? ''
  return [
    { value: '', label: 'Selecciona la visita' },
    ...visits.value
      .filter((visit) => visit.visitDate > matingDate)
      .map((visit) => ({ value: String(visit.id), label: `${formatDate(visit.visitDate)} · ${visit.vetName}` })),
  ]
})

const columns = [
  { key: 'visualTag' as const, label: 'Hembra' },
  { key: 'speciesId' as const, label: 'Especie' },
  { key: 'lotCode' as const, label: 'Lote' },
  { key: 'breeding' as const, label: 'Estado' },
  { key: 'daysToDue' as const, label: 'Parto probable' },
  { key: 'birthCount' as const, label: 'Partos' },
  { key: 'animalId' as const, label: 'Acciones' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[females.value, visits.value] = await Promise.all([listFemales(), listVisits()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

function openCheck(female: FemaleStatus): void {
  checkTarget.value = female
  Object.assign(checkForm, { visitId: '', result: 'pregnant', notes: '' })
  checkError.value = ''
  checkOpen.value = true
}

async function saveCheck(): Promise<void> {
  const breeding = checkTarget.value?.breeding
  if (!breeding || savingCheck.value) return
  checkError.value = ''
  if (!checkForm.visitId) {
    checkError.value = 'Elige la visita en la que el veterinario la revisó.'
    return
  }
  savingCheck.value = true
  try {
    const updated = await addCheck(breeding.id, {
      vetVisitId: Number(checkForm.visitId),
      result: checkForm.result,
      notes: checkForm.notes.trim() || null,
    })
    checkOpen.value = false
    toast.value?.show(`${updated.femaleTag}: ${outcomeLabels[updated.outcome].toLowerCase()}.`)
    await load()
  } catch (cause) {
    checkError.value = getApiErrorMessage(cause)
  } finally {
    savingCheck.value = false
  }
}

onMounted(async () => {
  await load()
  if (typeof route.query.guardado === 'string' && route.query.guardado) {
    toast.value?.show(route.query.guardado)
    void router.replace({ query: {} })
  }
})
</script>

<template>
  <UiBasePage fluid title="Reproducción" description="Montas naturales, diagnóstico de preñez del veterinario y partos.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'breeding-new' }">Registrar monta</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'birth-new' }">Registrar parto</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'reproduction-history' }">Historial</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!females" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="panel-chips" role="group" aria-label="Especie">
        <UiChip :pressed="species === 0" @click="species = 0">Todas</UiChip>
        <UiChip :pressed="species === 1" @click="species = 1">Vacas</UiChip>
        <UiChip :pressed="species === 2" @click="species = 2">Chivas</UiChip>
      </div>

      <div class="panel-metrics">
        <UiCard metric><UiMetric label="Cargadas" :value="counts.pregnant" /></UiCard>
        <UiCard metric><UiMetric label="Parto en 30 días" :value="counts.dueSoon" description="O ya vencido" /></UiCard>
        <UiCard metric><UiMetric label="Por diagnosticar" :value="counts.pending" description="Montadas sin revisión" /></UiCard>
        <UiCard metric><UiMetric label="Vacías" :value="counts.open" description="Listas para montar" /></UiCard>
      </div>

      <UiCard title="Hembras activas" :description="`${rows.length} de ${inSpecies.length}`">
        <div class="panel-filters">
          <UiField v-model="filter" as="select" label="Mostrar" :options="filterOptions" />
        </div>
        <p v-if="rows.length === 0" class="panel-muted">
          No hay hembras con este filtro.
          <RouterLink v-if="inSpecies.length === 0" class="panel-link" to="/panel/hato/nuevo">Registrar hembras en el hato</RouterLink>
        </p>
        <UiTable v-else label="Estado reproductivo de las hembras" :columns="columns" :rows="rows">
          <template #cell-visualTag="{ row }">
            <RouterLink class="panel-link" :to="`/panel/hato/${row.animalId}`">{{ row.visualTag }}</RouterLink>
          </template>
          <template #cell-speciesId="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
          <template #cell-lotCode="{ row }">{{ row.lotCode ?? '—' }}</template>
          <template #cell-breeding="{ row }">
            <template v-if="row.breeding">
              <UiBadge :tone="outcomeTone(row.breeding.outcome)">{{ outcomeLabels[row.breeding.outcome] }}</UiBadge>
              <span class="panel-muted"> · {{ row.breeding.maleTag }}, {{ formatDate(row.breeding.matingDate) }}</span>
            </template>
            <span v-else class="panel-muted">Sin montas</span>
          </template>
          <template #cell-daysToDue="{ row }">
            <template v-if="row.breeding && row.daysToDue !== null">
              {{ formatDate(row.breeding.expectedDueDate) }}
              <UiBadge v-if="row.breeding.outcome === 'pregnant' && row.daysToDue <= 30" tone="warning">{{ dueText(row.daysToDue) }}</UiBadge>
              <span v-else class="panel-muted"> {{ dueText(row.daysToDue) }}</span>
            </template>
            <span v-else class="panel-muted">—</span>
          </template>
          <template #cell-birthCount="{ row }">
            {{ row.birthCount }}<span v-if="row.lastBirthDate" class="panel-muted"> · último {{ formatDate(row.lastBirthDate) }}</span>
          </template>
          <template #cell-animalId="{ row }">
            <div class="rp-actions">
              <button v-if="row.breeding && ['pending', 'pregnant'].includes(row.breeding.outcome)" type="button" class="panel-icon-button panel-icon-button--neutral" @click="openCheck(row)">Diagnóstico</button>
              <RouterLink v-if="row.breeding && ['pending', 'pregnant'].includes(row.breeding.outcome)" class="panel-link" :to="{ name: 'birth-new', query: { madre: row.animalId } }">Parto</RouterLink>
              <RouterLink v-else class="panel-link" :to="{ name: 'breeding-new', query: { hembra: row.animalId } }">Montar</RouterLink>
              <RouterLink class="panel-link" :to="{ name: 'reproduction-history', query: { hembra: row.animalId } }">Historial</RouterLink>
            </div>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiDialog v-model="checkOpen" :title="`Diagnóstico de ${checkTarget?.visualTag ?? ''}`">
      <form id="rp-check-form" class="ui-form" novalidate @submit.prevent="saveCheck">
        <p v-if="checkTarget?.breeding">Monta del {{ formatDate(checkTarget.breeding.matingDate) }} con {{ checkTarget.breeding.maleTag }}.</p>
        <UiField v-model="checkForm.visitId" as="select" label="Visita del veterinario *" :options="visitOptions" />
        <p v-if="visitOptions.length === 1" class="panel-muted">
          No hay visitas después de la monta. <RouterLink class="panel-link" to="/panel/sanidad/visitas">Registrar visita</RouterLink>
        </p>
        <UiField v-model="checkForm.result" as="select" label="Resultado *" :options="checkResultOptions" />
        <UiField v-model="checkForm.notes" label="Notas" maxlength="255" placeholder="Ej. Palpación, 60 días" />
        <UiAlert v-if="checkError" tone="danger" title="No se guardó">{{ checkError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="rp-check-form" :loading="savingCheck">Guardar diagnóstico</UiButton>
        <UiButton variant="secondary" @click="checkOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.rp-actions { display: flex; align-items: center; gap: 4px 16px; flex-wrap: wrap; }
.rp-actions .panel-icon-button { padding: 0; }
</style>
