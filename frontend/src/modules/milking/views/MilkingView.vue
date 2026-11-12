<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiMetric, UiTable, UiToast } from '../../../shared/components'
import { formatDate, todayIso } from '../../../shared/utils/format'
import { createDay, getSummary, listDays, listGoats } from '../api/milkingApi'
import GoatChecklist from '../components/GoatChecklist.vue'
import type { MilkDaySummary, MilkingGoat, MilkSummary } from '../types/milking'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const summary = ref<MilkSummary | null>(null)
const days = ref<MilkDaySummary[] | null>(null)
const goats = ref<MilkingGoat[]>([])
const error = ref('')

const form = reactive({ date: today, liters: '', goatIds: [] as number[], notes: '' })
const saveError = ref('')
const saving = ref(false)
const loadingGoats = ref(false)

const existingDay = computed(() => days.value?.find((day) => day.collectionDate === form.date) ?? null)
const perGoat = computed(() => (Number(form.liters) > 0 && form.goatIds.length ? Number(form.liters) / form.goatIds.length : null))
const trend = computed(() => {
  if (!summary.value || summary.value.previous30Days.days === 0 || summary.value.last30Days.days === 0) return undefined
  const change = (summary.value.last30Days.averagePerDay / summary.value.previous30Days.averagePerDay - 1) * 100
  return `${change >= 0 ? '+' : ''}${change.toFixed(0)} % contra los 30 días anteriores`
})

const liters = (value: number) => `${value.toLocaleString('es-MX', { maximumFractionDigits: 2 })} L`
const columns = [
  { key: 'collectionDate' as const, label: 'Fecha' },
  { key: 'totalLiters' as const, label: 'Litros' },
  { key: 'goatCount' as const, label: 'Cabras' },
  { key: 'litersPerGoat' as const, label: 'Litros por cabra' },
  { key: 'code' as const, label: 'Lote de leche' },
  { key: 'notes' as const, label: 'Notas' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[summary.value, days.value] = await Promise.all([getSummary(), listDays()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

// Al cambiar la fecha se recalculan las cabras disponibles y se marcan todas las que se pueden ordeñar.
async function loadGoats(): Promise<void> {
  loadingGoats.value = true
  try {
    goats.value = await listGoats(form.date)
    form.goatIds = goats.value.filter((goat) => goat.canBeMilked).map((goat) => goat.animalId)
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    loadingGoats.value = false
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  const total = Number(form.liters)
  if (!form.date || !(total > 0)) {
    saveError.value = 'Indica la fecha y el total de litros.'
    return
  }
  if (form.goatIds.length === 0) {
    saveError.value = 'Marca al menos una cabra ordeñada.'
    return
  }
  saving.value = true
  try {
    const day = await createDay({ collectionDate: form.date, totalLiters: total, goatIds: form.goatIds, notes: form.notes.trim() || null })
    toast.value?.show(`Ordeña del ${formatDate(day.collectionDate)} registrada: ${liters(day.totalLiters)}, lote ${day.code}.`)
    form.liters = ''
    form.notes = ''
    await load()
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

watch(() => form.date, () => {
  if (form.date) void loadGoats()
})

onMounted(async () => {
  await Promise.all([load(), loadGoats()])
  if (typeof route.query.guardado === 'string' && route.query.guardado) {
    toast.value?.show(route.query.guardado)
    void router.replace({ query: {} })
  }
})
</script>

<template>
  <UiBasePage fluid title="Ordeña" description="Una ordeña al día: total de litros y cabras ordeñadas. La leche del día es el lote para quesería.">
    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!summary || !days" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="panel-metrics">
        <UiCard metric>
          <UiMetric
            label="Última ordeña"
            :value="summary.lastDay ? liters(summary.lastDay.totalLiters) : '—'"
            :description="summary.lastDay ? `${formatDate(summary.lastDay.collectionDate)} · ${summary.lastDay.goatCount} cabras` : 'Sin registros'"
          />
        </UiCard>
        <UiCard metric><UiMetric label="Promedio diario, 7 días" :value="liters(summary.last7Days.averagePerDay)" :description="`${liters(summary.last7Days.liters)} en ${summary.last7Days.days} ordeñas`" /></UiCard>
        <UiCard metric><UiMetric label="Promedio diario, 30 días" :value="liters(summary.last30Days.averagePerDay)" :description="trend ?? `${summary.last30Days.days} ordeñas`" /></UiCard>
      </div>

      <UiCard title="Registrar ordeña">
        <form class="stack" novalidate @submit.prevent="save">
          <div class="panel-form">
            <UiField v-model="form.date" label="Fecha *" type="date" :max="today" required />
            <template v-if="!existingDay">
              <UiField v-model="form.liters" label="Total de litros *" type="number" min="0.01" max="2000" step="0.01" inputmode="decimal" required />
              <UiField v-model="form.notes" label="Notas" maxlength="255" />
            </template>
          </div>
          <UiAlert v-if="existingDay" tone="info" title="Ese día ya tiene ordeña">
            {{ liters(existingDay.totalLiters) }} de {{ existingDay.goatCount }} cabras.
            <RouterLink class="panel-link" :to="{ name: 'milking-day', params: { id: existingDay.id } }">Editarla</RouterLink>
            o elige otra fecha.
          </UiAlert>
          <template v-else>
            <p v-if="loadingGoats" aria-busy="true">Revisando cabras…</p>
            <GoatChecklist v-else v-model="form.goatIds" :goats="goats" :date="form.date" />
            <UiAlert v-if="saveError" tone="danger" title="No se registró">{{ saveError }}</UiAlert>
            <div class="panel-actions">
              <UiButton type="submit" :loading="saving">Registrar ordeña</UiButton>
              <span v-if="perGoat !== null" class="panel-muted">≈ {{ liters(Number(perGoat.toFixed(2))) }} por cabra</span>
            </div>
          </template>
        </form>
      </UiCard>

      <UiCard title="Ordeñas recientes" description="Últimos 60 días.">
        <p v-if="days.length === 0" class="panel-muted">Aún no hay ordeñas registradas.</p>
        <UiTable v-else label="Ordeñas de los últimos 60 días" :columns="columns" :rows="days">
          <template #cell-collectionDate="{ row }">
            <RouterLink class="panel-link" :to="{ name: 'milking-day', params: { id: row.id } }">{{ formatDate(row.collectionDate) }}</RouterLink>
          </template>
          <template #cell-totalLiters="{ row }"><strong>{{ liters(row.totalLiters) }}</strong></template>
          <template #cell-litersPerGoat="{ row }">{{ liters(row.litersPerGoat) }}</template>
          <template #cell-notes="{ row }">{{ row.notes ?? '—' }}</template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
