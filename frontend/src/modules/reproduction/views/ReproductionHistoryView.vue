<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBadge, UiBasePage, UiCard, UiField, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import { formatDate } from '../../../shared/utils/format'
import { listAnimals, type AnimalSummary } from '../../herd'
import { deleteBreeding, listBirths, listBreedings } from '../api/reproductionApi'
import { checkResultOptions, outcomeLabels, outcomeTone } from '../data/labels'
import type { BirthItem, BreedingItem } from '../types/reproduction'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const breedings = ref<BreedingItem[] | null>(null)
const births = ref<BirthItem[] | null>(null)
const females = ref<AnimalSummary[]>([])
const error = ref('')
const deletingId = ref<number | null>(null)

const femaleId = computed(() => {
  const value = Number(route.query.hembra)
  return value > 0 ? value : undefined
})
const femaleTag = computed(() => females.value.find((animal) => animal.id === femaleId.value)?.visualTag)
const femaleOptions = computed(() => [
  { value: '', label: 'Todas' },
  ...females.value.map((animal) => ({ value: String(animal.id), label: animal.visualTag })),
])
const resultLabel = (result: string) => checkResultOptions.find((option) => option.value === result)?.label ?? result

const breedingColumns = [
  { key: 'matingDate' as const, label: 'Monta' },
  { key: 'femaleTag' as const, label: 'Hembra' },
  { key: 'maleTag' as const, label: 'Semental' },
  { key: 'outcome' as const, label: 'Estado' },
  { key: 'checks' as const, label: 'Diagnósticos' },
  { key: 'expectedDueDate' as const, label: 'Parto probable' },
  { key: 'id' as const, label: 'Acciones' },
]
const birthColumns = [
  { key: 'birthDate' as const, label: 'Parto' },
  { key: 'damTag' as const, label: 'Madre' },
  { key: 'sireTag' as const, label: 'Padre' },
  { key: 'offspring' as const, label: 'Crías vivas' },
  { key: 'stillbornCount' as const, label: 'Muertas' },
  { key: 'complications' as const, label: 'Complicaciones' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[breedings.value, births.value] = await Promise.all([
      listBreedings({ femaleId: femaleId.value }),
      listBirths({ damId: femaleId.value }),
    ])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function remove(breeding: BreedingItem): Promise<void> {
  if (deletingId.value !== null) return
  deletingId.value = breeding.id
  try {
    await deleteBreeding(breeding.id)
    toast.value?.show(`Monta de ${breeding.femaleTag} del ${formatDate(breeding.matingDate)} eliminada.`)
    await load()
  } catch (cause) {
    toast.value?.show(getApiErrorMessage(cause))
  } finally {
    deletingId.value = null
  }
}

function setFemale(value: string): void {
  void router.replace({ query: value ? { hembra: value } : {} })
}

watch(femaleId, load)

onMounted(async () => {
  await load()
  if (typeof route.query.guardado === 'string' && route.query.guardado) {
    toast.value?.show(route.query.guardado)
    void router.replace({ query: femaleId.value ? { hembra: String(femaleId.value) } : {} })
  }
  try {
    females.value = (await listAnimals({ sex: 'F', status: 'all', pageSize: 200 })).items
  } catch {
    // Sin la lista, el filtro solo muestra «Todas».
  }
})
</script>

<template>
  <UiBasePage fluid title="Historial reproductivo" :description="femaleTag ? `Montas y partos de ${femaleTag}.` : 'Montas y partos, del más reciente al más antiguo.'">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink v-if="femaleId" class="btn secondary" :to="`/panel/hato/${femaleId}`">Ficha de la hembra</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Volver a reproducción</RouterLink>
      </div>
    </template>

    <div class="stack">
      <div class="panel-filters">
        <UiField as="select" label="Hembra" :options="femaleOptions" :model-value="femaleId ? String(femaleId) : ''" @update:model-value="setFemale" />
      </div>

      <UiLoadError v-if="error" :message="error" @retry="load" />
      <p v-else-if="!breedings || !births" aria-busy="true">Cargando…</p>
      <template v-else>
        <UiCard title="Partos" :description="`${births.length} registrado${births.length === 1 ? '' : 's'}`">
          <p v-if="births.length === 0" class="panel-muted">Sin partos registrados.</p>
          <UiTable v-else label="Partos" :columns="birthColumns" :rows="births">
            <template #cell-birthDate="{ row }">{{ formatDate(row.birthDate) }}</template>
            <template #cell-damTag="{ row }"><RouterLink class="panel-link" :to="`/panel/hato/${row.damId}`">{{ row.damTag }}</RouterLink></template>
            <template #cell-sireTag="{ row }">{{ row.sireTag ?? 'Desconocido' }}</template>
            <template #cell-offspring="{ row }">
              <template v-if="row.offspring.length">
                <span v-for="(young, index) in row.offspring" :key="young.animalId">
                  {{ index ? ', ' : '' }}<RouterLink class="panel-link" :to="`/panel/hato/${young.animalId}`">{{ young.visualTag }}</RouterLink>
                  ({{ young.sex === 'M' ? 'macho' : 'hembra' }})
                </span>
              </template>
              <span v-else class="panel-muted">Ninguna</span>
            </template>
            <template #cell-complications="{ row }">{{ row.complications ?? '—' }}</template>
          </UiTable>
        </UiCard>

        <UiCard title="Montas" :description="`${breedings.length} registrada${breedings.length === 1 ? '' : 's'}`">
          <p v-if="breedings.length === 0" class="panel-muted">Sin montas registradas.</p>
          <UiTable v-else label="Montas" :columns="breedingColumns" :rows="breedings">
            <template #cell-matingDate="{ row }">{{ formatDate(row.matingDate) }}</template>
            <template #cell-femaleTag="{ row }"><RouterLink class="panel-link" :to="`/panel/hato/${row.femaleId}`">{{ row.femaleTag }}</RouterLink></template>
            <template #cell-maleTag="{ row }"><RouterLink class="panel-link" :to="`/panel/hato/${row.maleId}`">{{ row.maleTag }}</RouterLink></template>
            <template #cell-outcome="{ row }"><UiBadge :tone="outcomeTone(row.outcome)">{{ outcomeLabels[row.outcome] }}</UiBadge></template>
            <template #cell-checks="{ row }">
              <template v-if="row.checks.length">
                <span v-for="(check, index) in row.checks" :key="check.id">
                  {{ index ? '; ' : '' }}{{ formatDate(check.checkDate) }}: {{ resultLabel(check.result).toLowerCase() }} ({{ check.vetName }})
                </span>
              </template>
              <span v-else class="panel-muted">—</span>
            </template>
            <template #cell-expectedDueDate="{ row }">{{ formatDate(row.expectedDueDate) }}</template>
            <template #cell-id="{ row }">
              <button
                v-if="row.outcome === 'pending' && row.checks.length === 0"
                type="button"
                class="panel-icon-button"
                :disabled="deletingId !== null"
                :aria-label="`Eliminar monta de ${row.femaleTag} del ${formatDate(row.matingDate)}`"
                @click="remove(row)"
              >
                Eliminar
              </button>
              <span v-else class="panel-muted">—</span>
            </template>
          </UiTable>
        </UiCard>
      </template>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
