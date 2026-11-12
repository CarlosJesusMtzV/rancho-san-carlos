<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiChip, UiField, UiLoadError, UiMetric, UiTable } from '../../../shared/components'
import { listAnimals } from '../api/herdApi'
import LifecycleBadge from '../components/LifecycleBadge.vue'
import SpeciesTag from '../components/SpeciesTag.vue'
import { useCatalog } from '../composables/useCatalog'
import { formatAge, formatKg, sexLabels, stageLabel } from '../data/labels'
import type { AnimalPage, AnimalQuery, Sex, SpeciesId, StatusFilter } from '../types/herd'

const route = useRoute()
const router = useRouter()
const { catalog, hasBreeds, lotsFor, load: loadCatalog, error: catalogError } = useCatalog()

const page = ref<AnimalPage | null>(null)
const loading = ref(false)
const error = ref('')
const totals = ref<Record<SpeciesId, number | null>>({ 1: null, 2: null })
const searchText = ref('')

// Los filtros viven en la URL: se pueden compartir y el botón Atrás los respeta.
const filters = computed<AnimalQuery>(() => {
  const query = route.query
  const species = Number(query.especie)
  const lot = Number(query.lote)
  const pageNumber = Number(query.pagina)
  return {
    speciesId: species === 1 || species === 2 ? species : undefined,
    lotId: Number.isInteger(lot) && lot > 0 ? lot : undefined,
    sex: query.sexo === 'M' || query.sexo === 'F' ? query.sexo : undefined,
    status: query.estado === 'inactive' || query.estado === 'all' ? query.estado : 'active',
    search: typeof query.buscar === 'string' && query.buscar.trim() ? query.buscar.trim() : undefined,
    page: Number.isInteger(pageNumber) && pageNumber > 1 ? pageNumber : 1,
    pageSize: 50,
  }
})

function setFilters(changes: Record<string, string | undefined>): void {
  const next: Record<string, string> = {}
  for (const [key, value] of Object.entries({ ...route.query, ...changes, pagina: changes.pagina })) {
    if (typeof value === 'string' && value !== '') next[key] = value
  }
  void router.replace({ query: next })
}

const speciesOptions: { value: SpeciesId | undefined; label: string }[] = [
  { value: undefined, label: 'Todas' },
  { value: 1, label: 'Bovinos' },
  { value: 2, label: 'Caprinos' },
]

const statusOptions = [
  { value: 'active', label: 'Activos' },
  { value: 'inactive', label: 'Bajas' },
  { value: 'all', label: 'Todos' },
]

const sexOptions = [
  { value: '', label: 'Ambos' },
  { value: 'M', label: 'Machos' },
  { value: 'F', label: 'Hembras' },
]

const lotOptions = computed(() => [
  { value: '', label: 'Todos' },
  ...lotsFor(filters.value.speciesId).map((lot) => ({ value: String(lot.id), label: lot.code })),
])

const columns = [
  { key: 'visualTag' as const, label: 'Arete' },
  { key: 'speciesName' as const, label: 'Especie' },
  { key: 'breedName' as const, label: 'Raza' },
  { key: 'sex' as const, label: 'Sexo' },
  { key: 'productionStage' as const, label: 'Etapa' },
  { key: 'ageMonths' as const, label: 'Edad' },
  { key: 'lotCode' as const, label: 'Lote' },
  { key: 'lastWeightKg' as const, label: 'Último peso' },
  { key: 'lifecycleStatus' as const, label: 'Estado' },
]

const totalPages = computed(() => (page.value ? Math.max(1, Math.ceil(page.value.total / page.value.pageSize)) : 1))

let requestId = 0
async function loadAnimals(): Promise<void> {
  const current = ++requestId
  loading.value = true
  error.value = ''
  try {
    const result = await listAnimals(filters.value)
    if (current === requestId) page.value = result
  } catch (cause) {
    if (current === requestId) error.value = getApiErrorMessage(cause)
  } finally {
    if (current === requestId) loading.value = false
  }
}

async function loadTotals(): Promise<void> {
  try {
    const [cattle, goats] = await Promise.all([
      listAnimals({ speciesId: 1, status: 'active', pageSize: 1 }),
      listAnimals({ speciesId: 2, status: 'active', pageSize: 1 }),
    ])
    totals.value = { 1: cattle.total, 2: goats.total }
  } catch {
    // Los totales son informativos; el error principal ya lo muestra el listado.
  }
}

function submitSearch(): void {
  setFilters({ buscar: searchText.value.trim() || undefined })
}

watch(filters, () => {
  searchText.value = filters.value.search ?? ''
  void loadAnimals()
}, { immediate: true })

onMounted(() => {
  void loadCatalog()
  void loadTotals()
})
</script>

<template>
  <UiBasePage fluid title="Hato" description="Bovinos para carne y caprinos para cabritos y leche.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'herd-new' }">Registrar animal</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'herd-calves' }">Becerros para venta</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'herd-catalog' }">Razas y lotes</RouterLink>
      </div>
    </template>

    <div class="stack">
      <UiAlert v-if="catalog && !hasBreeds" tone="warning" title="Primero registra las razas">
        Para dar de alta animales necesitas al menos una raza por especie.
        <RouterLink class="panel-link" :to="{ name: 'herd-catalog' }">Ir a razas y lotes</RouterLink>
      </UiAlert>
      <UiAlert v-if="catalogError" tone="warning" title="Catálogos no disponibles">{{ catalogError }}</UiAlert>

      <div class="panel-metrics">
        <UiCard metric><UiMetric label="Bovinos activos" :value="totals[1] ?? '—'" description="Becerros, vacas y toros" /></UiCard>
        <UiCard metric><UiMetric label="Caprinos activos" :value="totals[2] ?? '—'" description="Cabritos, cabras y sementales" /></UiCard>
      </div>

      <UiCard title="Animales" :description="page ? `${page.total} resultado${page.total === 1 ? '' : 's'}` : undefined">
        <div class="panel-chips" role="group" aria-label="Especie">
          <UiChip
            v-for="option in speciesOptions"
            :key="option.label"
            :pressed="filters.speciesId === option.value"
            @click="setFilters({ especie: option.value ? String(option.value) : undefined, lote: undefined })"
          >
            {{ option.label }}
          </UiChip>
        </div>

        <form class="panel-filters" role="search" @submit.prevent="submitSearch">
          <UiField
            as="select"
            label="Estado"
            :options="statusOptions"
            :model-value="filters.status"
            @update:model-value="setFilters({ estado: $event === 'active' ? undefined : ($event as StatusFilter) })"
          />
          <UiField
            as="select"
            label="Sexo"
            :options="sexOptions"
            :model-value="filters.sex ?? ''"
            @update:model-value="setFilters({ sexo: ($event as Sex) || undefined })"
          />
          <UiField
            as="select"
            label="Lote"
            :options="lotOptions"
            :model-value="filters.lotId ? String(filters.lotId) : ''"
            @update:model-value="setFilters({ lote: $event || undefined })"
          />
          <UiField v-model="searchText" label="Buscar arete" type="search" placeholder="Ej. SC-BEC" autocomplete="off" />
          <UiButton type="submit" variant="secondary">Buscar</UiButton>
        </form>

        <UiLoadError v-if="error" :message="error" @retry="loadAnimals" />
        <p v-else-if="loading && !page" aria-busy="true">Cargando…</p>
        <p v-else-if="page && page.items.length === 0" class="panel-muted">
          No hay animales con estos filtros.
          <RouterLink v-if="hasBreeds" class="panel-link" :to="{ name: 'herd-new' }">Registrar el primero</RouterLink>
        </p>
        <template v-else-if="page">
          <UiTable label="Animales del hato" :columns="columns" :rows="page.items" :aria-busy="loading || undefined">
            <template #cell-visualTag="{ row }">
              <RouterLink class="panel-link" :to="{ name: 'herd-animal', params: { id: row.id } }">{{ row.visualTag }}</RouterLink>
            </template>
            <template #cell-speciesName="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
            <template #cell-sex="{ row }">{{ sexLabels[row.sex] }}</template>
            <template #cell-productionStage="{ row }">{{ stageLabel(row.productionStage) }}</template>
            <template #cell-ageMonths="{ row }">{{ formatAge(row.ageMonths) }}</template>
            <template #cell-lotCode="{ row }">{{ row.lotCode ?? '—' }}</template>
            <template #cell-lastWeightKg="{ row }">{{ formatKg(row.lastWeightKg) }}</template>
            <template #cell-lifecycleStatus="{ row }"><LifecycleBadge :status="row.lifecycleStatus" /></template>
          </UiTable>
          <nav v-if="totalPages > 1" class="panel-pager" aria-label="Páginas">
            <UiButton variant="secondary" :disabled="(filters.page ?? 1) <= 1" @click="setFilters({ pagina: String((filters.page ?? 1) - 1) })">
              Anterior
            </UiButton>
            <span>Página {{ filters.page }} de {{ totalPages }}</span>
            <UiButton variant="secondary" :disabled="(filters.page ?? 1) >= totalPages" @click="setFilters({ pagina: String((filters.page ?? 1) + 1) })">
              Siguiente
            </UiButton>
          </nav>
        </template>
      </UiCard>
    </div>
  </UiBasePage>
</template>
