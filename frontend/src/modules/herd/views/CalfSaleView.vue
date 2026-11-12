<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBadge, UiBasePage, UiCard, UiMetric, UiTable } from '../../../shared/components'
import { getCalfSaleCandidates } from '../api/herdApi'
import LoadError from '../components/LoadError.vue'
import { formatAge, formatDate, formatKg, sexLabels } from '../data/labels'
import type { CalfSaleCandidate } from '../types/herd'
import '../styles.css'

const calves = ref<CalfSaleCandidate[] | null>(null)
const error = ref('')

// La tabla requiere una llave id.
const rows = computed(() => (calves.value ?? []).map((calf) => ({ ...calf, id: calf.animalId })))
const ready = computed(() => rows.value.filter((calf) => calf.reachedSaleTarget).length)

const columns = [
  { key: 'visualTag' as const, label: 'Arete' },
  { key: 'reachedSaleTarget' as const, label: 'Meta' },
  { key: 'sex' as const, label: 'Sexo' },
  { key: 'ageMonths' as const, label: 'Edad' },
  { key: 'lastWeightKg' as const, label: 'Último peso' },
  { key: 'lotCode' as const, label: 'Lote' },
  { key: 'meatAllowedFrom' as const, label: 'Retiro sanitario' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    calves.value = await getCalfSaleCandidates()
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage
    fluid
    title="Becerros para venta"
    description="Bovinos activos menores de 18 meses sin vender. La meta es una edad promedio de 8 meses o un peso aproximado de 150 kg."
  >
    <template #actions>
      <div class="herd-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'herd' }">Volver al hato</RouterLink>
      </div>
    </template>

    <LoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!calves" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="herd-metrics">
        <UiCard metric><UiMetric label="Becerros en crecimiento" :value="rows.length" /></UiCard>
        <UiCard metric><UiMetric label="Alcanzaron la meta" :value="ready" description="8 meses o 150 kg" /></UiCard>
      </div>

      <UiCard title="Becerros">
        <p v-if="rows.length === 0" class="herd-muted">No hay becerros en crecimiento registrados.</p>
        <UiTable v-else label="Becerros rumbo a la venta" :columns="columns" :rows="rows">
          <template #cell-visualTag="{ row }">
            <RouterLink class="herd-link" :to="{ name: 'herd-animal', params: { id: row.animalId } }">{{ row.visualTag }}</RouterLink>
          </template>
          <template #cell-reachedSaleTarget="{ row }">
            <UiBadge :tone="row.reachedSaleTarget ? 'success' : 'info'">{{ row.reachedSaleTarget ? 'Lista para venta' : 'En crecimiento' }}</UiBadge>
          </template>
          <template #cell-sex="{ row }">{{ sexLabels[row.sex] }}</template>
          <template #cell-ageMonths="{ row }">{{ formatAge(row.ageMonths) }}</template>
          <template #cell-lastWeightKg="{ row }">
            {{ formatKg(row.lastWeightKg) }}<span v-if="row.lastWeighedOn" class="herd-muted"> · {{ formatDate(row.lastWeighedOn) }}</span>
          </template>
          <template #cell-lotCode="{ row }">{{ row.lotCode ?? '—' }}</template>
          <template #cell-meatAllowedFrom="{ row }">
            <UiBadge v-if="row.meatAllowedFrom" tone="warning">Hasta {{ formatDate(row.meatAllowedFrom) }}</UiBadge>
            <template v-else>Sin retiro</template>
          </template>
        </UiTable>
      </UiCard>
    </div>
  </UiBasePage>
</template>
