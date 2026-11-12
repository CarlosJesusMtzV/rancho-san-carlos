<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiToast } from '../../../shared/components'
import { formatDate } from '../../../shared/utils/format'
import { getHerdCatalog, listAnimals, type AnimalSummary, type LotItem } from '../../herd'
import { deleteEvent, listEvents } from '../api/healthApi'
import HealthEventsTable from '../components/HealthEventsTable.vue'
import { eventTypeLabels, eventTypeOptions } from '../data/labels'
import type { HealthEventItem, HealthEventPage, HealthEventQuery, HealthEventType } from '../types/health'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const page = ref<HealthEventPage | null>(null)
const error = ref('')
const loading = ref(false)
const animals = ref<AnimalSummary[]>([])
const lots = ref<LotItem[]>([])
const range = reactive({ from: '', to: '' })

const pendingDelete = ref<HealthEventItem | null>(null)
const deleteOpen = ref(false)
const deleting = ref(false)
const deleteError = ref('')

const filters = computed<HealthEventQuery>(() => {
  const query = route.query
  const animal = Number(query.animal)
  const lot = Number(query.lote)
  const pageNumber = Number(query.pagina)
  const type = typeof query.tipo === 'string' && query.tipo in eventTypeLabels ? (query.tipo as HealthEventType) : undefined
  const date = (value: unknown) => (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : undefined)
  return {
    animalId: animal > 0 ? animal : undefined,
    lotId: lot > 0 ? lot : undefined,
    type,
    from: date(query.desde),
    to: date(query.hasta),
    page: pageNumber > 1 ? pageNumber : 1,
    pageSize: 50,
  }
})

function setFilters(changes: Record<string, string | undefined>): void {
  const next: Record<string, string> = {}
  for (const [key, value] of Object.entries({ ...route.query, ...changes, pagina: changes.pagina, registrados: undefined })) {
    if (typeof value === 'string' && value !== '') next[key] = value
  }
  void router.replace({ query: next })
}

const animalOptions = computed(() => [
  { value: '', label: 'Todos' },
  ...animals.value.map((animal) => ({ value: String(animal.id), label: animal.visualTag })),
])
const lotOptions = computed(() => [{ value: '', label: 'Todos' }, ...lots.value.map((lot) => ({ value: String(lot.id), label: lot.code }))])
const typeOptions = [{ value: '', label: 'Todos' }, ...eventTypeOptions]
const totalPages = computed(() => (page.value ? Math.max(1, Math.ceil(page.value.total / page.value.pageSize)) : 1))
const animalTag = computed(() => animals.value.find((animal) => animal.id === filters.value.animalId)?.visualTag)

let requestId = 0
async function load(): Promise<void> {
  const current = ++requestId
  loading.value = true
  error.value = ''
  try {
    const result = await listEvents(filters.value)
    if (current === requestId) page.value = result
  } catch (cause) {
    if (current === requestId) error.value = getApiErrorMessage(cause)
  } finally {
    if (current === requestId) loading.value = false
  }
}

function askDelete(event: HealthEventItem): void {
  pendingDelete.value = event
  deleteError.value = ''
  deleteOpen.value = true
}

async function confirmDelete(): Promise<void> {
  if (!pendingDelete.value || deleting.value) return
  deleting.value = true
  deleteError.value = ''
  try {
    await deleteEvent(pendingDelete.value.id)
    deleteOpen.value = false
    toast.value?.show('Evento eliminado. Ya no cuenta para los retiros.')
    await load()
  } catch (cause) {
    deleteError.value = getApiErrorMessage(cause)
  } finally {
    deleting.value = false
  }
}

watch(filters, () => {
  range.from = filters.value.from ?? ''
  range.to = filters.value.to ?? ''
  void load()
}, { immediate: true })

onMounted(async () => {
  const registered = Number(route.query.registrados)
  if (registered > 0) {
    toast.value?.show(`${registered} evento${registered === 1 ? '' : 's'} registrado${registered === 1 ? '' : 's'}.`)
    setFilters({})
  }
  try {
    const [animalPage, catalog] = await Promise.all([listAnimals({ status: 'all', pageSize: 200 }), getHerdCatalog()])
    animals.value = animalPage.items
    lots.value = catalog.lots
  } catch {
    // Los filtros por animal y lote quedan vacíos; el historial sigue funcionando.
  }
})
</script>

<template>
  <UiBasePage fluid title="Historial sanitario" :description="animalTag ? `Eventos de ${animalTag}.` : 'Todos los eventos, del más reciente al más antiguo.'">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'health-event-new', query: filters.animalId ? { animal: filters.animalId } : {} }">Registrar evento</RouterLink>
        <RouterLink v-if="filters.animalId" class="btn secondary" :to="`/panel/hato/${filters.animalId}`">Ficha del animal</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'health' }">Volver a sanidad</RouterLink>
      </div>
    </template>

    <UiCard title="Eventos" :description="page ? `${page.total} resultado${page.total === 1 ? '' : 's'}` : undefined">
      <form class="panel-filters" role="search" @submit.prevent="setFilters({ desde: range.from || undefined, hasta: range.to || undefined })">
        <UiField as="select" label="Animal" :options="animalOptions" :model-value="filters.animalId ? String(filters.animalId) : ''" @update:model-value="setFilters({ animal: $event || undefined })" />
        <UiField as="select" label="Lote" :options="lotOptions" :model-value="filters.lotId ? String(filters.lotId) : ''" @update:model-value="setFilters({ lote: $event || undefined })" />
        <UiField as="select" label="Tipo" :options="typeOptions" :model-value="filters.type ?? ''" @update:model-value="setFilters({ tipo: $event || undefined })" />
        <UiField v-model="range.from" label="Desde" type="date" />
        <UiField v-model="range.to" label="Hasta" type="date" />
        <UiButton type="submit" variant="secondary">Aplicar fechas</UiButton>
      </form>

      <UiLoadError v-if="error" :message="error" @retry="load" />
      <p v-else-if="loading && !page" aria-busy="true">Cargando…</p>
      <p v-else-if="page && page.items.length === 0" class="panel-muted">No hay eventos con estos filtros.</p>
      <template v-else-if="page">
        <HealthEventsTable label="Historial sanitario" :events="page.items" deletable :busy-id="deleting ? pendingDelete?.id ?? null : null" @remove="askDelete" />
        <nav v-if="totalPages > 1" class="panel-pager" aria-label="Páginas">
          <UiButton variant="secondary" :disabled="(filters.page ?? 1) <= 1" @click="setFilters({ pagina: String((filters.page ?? 1) - 1) })">Anterior</UiButton>
          <span>Página {{ filters.page }} de {{ totalPages }}</span>
          <UiButton variant="secondary" :disabled="(filters.page ?? 1) >= totalPages" @click="setFilters({ pagina: String((filters.page ?? 1) + 1) })">Siguiente</UiButton>
        </nav>
      </template>
    </UiCard>

    <UiDialog v-model="deleteOpen" title="Eliminar evento">
      <p v-if="pendingDelete">
        Se eliminará la {{ eventTypeLabels[pendingDelete.eventType].toLowerCase() }} de <strong>{{ pendingDelete.visualTag }}</strong>
        del {{ formatDate(pendingDelete.eventDate) }} con sus medicamentos. Sus retiros dejarán de contar.
      </p>
      <UiAlert v-if="deleteError" tone="danger" title="No se eliminó">{{ deleteError }}</UiAlert>
      <template #actions>
        <UiButton variant="danger" :loading="deleting" @click="confirmDelete">Eliminar evento</UiButton>
        <UiButton variant="secondary" @click="deleteOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
