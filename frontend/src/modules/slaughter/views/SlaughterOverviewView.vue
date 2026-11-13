<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBasePage, UiCard, UiLoadError, UiMetric, UiTable, UiToast } from '../../../shared/components'
import { formatDate, formatMoney } from '../../../shared/utils/format'
import { getSummary, listCarcasses, listRuns } from '../api/slaughterApi'
import type { Carcass, SlaughterRunSummary, SlaughterSummary } from '../types/slaughter'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const summary = ref<SlaughterSummary | null>(null)
const carcasses = ref<Carcass[] | null>(null)
const runs = ref<SlaughterRunSummary[] | null>(null)
const error = ref('')

const kg = (value: number) => `${value.toLocaleString('es-MX', { maximumFractionDigits: 2 })} kg`
const daysText = (days: number) => (days === 0 ? 'hoy' : days === 1 ? 'ayer' : `hace ${days} días`)

const carcassColumns = [
  { key: 'visualTag' as const, label: 'Cabrito' },
  { key: 'carcassWeightKg' as const, label: 'Canal' },
  { key: 'runDate' as const, label: 'Matanza' },
  { key: 'slaughterhouse' as const, label: 'Rastro o matancero' },
]
const runColumns = [
  { key: 'runDate' as const, label: 'Fecha' },
  { key: 'slaughterhouse' as const, label: 'Rastro o matancero' },
  { key: 'kidCount' as const, label: 'Cabritos' },
  { key: 'carcassWeightKg' as const, label: 'Canal' },
  { key: 'yieldPercent' as const, label: 'Rendimiento' },
  { key: 'cost' as const, label: 'Pago' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[summary.value, carcasses.value, runs.value] = await Promise.all([getSummary(), listCarcasses(), listRuns()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
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
  <UiBasePage fluid title="Matanza de cabritos" description="Jornadas en rastro o con matancero externo. La canal se vende entera.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'slaughter-new' }">Registrar matanza</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!summary || !carcasses || !runs" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="panel-metrics">
        <UiCard metric><UiMetric label="Canales disponibles" :value="summary.availableCarcasses" :description="kg(summary.availableCarcassKg)" /></UiCard>
        <UiCard metric><UiMetric label="Cabritos en 30 días" :value="summary.kidsLast30Days" /></UiCard>
        <UiCard metric>
          <UiMetric label="Rendimiento en canal" :value="summary.averageYieldPercent === null ? '—' : `${summary.averageYieldPercent} %`" description="Promedio de 180 días" />
        </UiCard>
        <UiCard metric>
          <UiMetric
            label="Última matanza"
            :value="summary.lastRun ? formatDate(summary.lastRun.runDate) : '—'"
            :description="summary.lastRun ? `${summary.lastRun.kidCount} cabritos · ${summary.lastRun.slaughterhouse}` : 'Sin registros'"
          />
        </UiCard>
      </div>

      <UiCard title="Canales sin vender" description="De la más antigua a la más reciente.">
        <p v-if="carcasses.length === 0" class="panel-muted">No hay canales disponibles.</p>
        <UiTable v-else label="Canales sin vender" :columns="carcassColumns" :rows="carcasses.map((item) => ({ ...item, id: item.slaughterId }))">
          <template #cell-visualTag="{ row }"><RouterLink class="panel-link" :to="`/panel/hato/${row.animalId}`">{{ row.visualTag }}</RouterLink></template>
          <template #cell-carcassWeightKg="{ row }"><strong>{{ kg(row.carcassWeightKg) }}</strong></template>
          <template #cell-runDate="{ row }">
            <RouterLink class="panel-link" :to="{ name: 'slaughter-run', params: { id: row.runId } }">{{ formatDate(row.runDate) }}</RouterLink>
            <span class="panel-muted"> · {{ daysText(row.daysSinceSlaughter) }}</span>
          </template>
        </UiTable>
      </UiCard>

      <UiCard title="Jornadas" description="Últimos 180 días.">
        <p v-if="runs.length === 0" class="panel-muted">
          Aún no hay matanzas registradas.
          <RouterLink class="panel-link" :to="{ name: 'slaughter-new' }">Registrar la primera</RouterLink>
        </p>
        <UiTable v-else label="Jornadas de matanza" :columns="runColumns" :rows="runs">
          <template #cell-runDate="{ row }"><RouterLink class="panel-link" :to="{ name: 'slaughter-run', params: { id: row.id } }">{{ formatDate(row.runDate) }}</RouterLink></template>
          <template #cell-carcassWeightKg="{ row }">{{ kg(row.carcassWeightKg) }} <span class="panel-muted">de {{ kg(row.liveWeightKg) }} vivo</span></template>
          <template #cell-yieldPercent="{ row }">{{ row.yieldPercent }} %</template>
          <template #cell-cost="{ row }">
            <template v-if="row.cost !== null">{{ formatMoney(row.cost) }} <span class="panel-muted">· {{ formatMoney(row.costPerKid) }} c/u</span></template>
            <span v-else class="panel-muted">—</span>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
