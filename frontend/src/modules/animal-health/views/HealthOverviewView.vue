<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBadge, UiBasePage, UiCard, UiLoadError, UiMetric, UiTable } from '../../../shared/components'
import { formatDate, formatMoney } from '../../../shared/utils/format'
import { SpeciesTag } from '../../herd'
import { listVisits, listWithdrawals } from '../api/healthApi'
import type { ActiveWithdrawal, VetVisitSummary } from '../types/health'

const withdrawals = ref<ActiveWithdrawal[] | null>(null)
const visits = ref<VetVisitSummary[] | null>(null)
const error = ref('')

const withdrawalRows = computed(() => (withdrawals.value ?? []).map((row) => ({ ...row, id: row.animalId })))
const recentVisits = computed(() => (visits.value ?? []).slice(0, 5))
const goatsWithMilkWithdrawal = computed(() => withdrawalRows.value.filter((row) => row.speciesId === 2 && row.milkAllowedFrom).length)

const withdrawalColumns = [
  { key: 'visualTag' as const, label: 'Arete' },
  { key: 'speciesId' as const, label: 'Especie' },
  { key: 'lotCode' as const, label: 'Lote' },
  { key: 'milkAllowedFrom' as const, label: 'Leche a partir de' },
  { key: 'meatAllowedFrom' as const, label: 'Carne a partir de' },
]
const visitColumns = [
  { key: 'visitDate' as const, label: 'Fecha' },
  { key: 'vetName' as const, label: 'Veterinario' },
  { key: 'animalCount' as const, label: 'Animales' },
  { key: 'total' as const, label: 'Pagado' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[withdrawals.value, visits.value] = await Promise.all([listWithdrawals(), listVisits()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Sanidad" description="Retiros sanitarios vigentes y visitas del veterinario.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'health-event-new' }">Registrar evento</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'health-visits' }">Visitas</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'health-history' }">Historial</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!withdrawals || !visits" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="panel-metrics">
        <UiCard metric><UiMetric label="Animales con retiro" :value="withdrawalRows.length" description="Leche o carne" /></UiCard>
        <UiCard metric><UiMetric label="Cabras sin ordeña para queso" :value="goatsWithMilkWithdrawal" description="Por retiro de leche" /></UiCard>
      </div>

      <UiCard title="Retiros vigentes" description="Hasta la fecha indicada la leche no va a quesería y el animal no se vende para carne.">
        <p v-if="withdrawalRows.length === 0" class="panel-muted">Ningún animal tiene retiro vigente.</p>
        <UiTable v-else label="Animales con retiro vigente" :columns="withdrawalColumns" :rows="withdrawalRows">
          <template #cell-visualTag="{ row }">
            <RouterLink class="panel-link" :to="`/panel/hato/${row.animalId}`">{{ row.visualTag }}</RouterLink>
          </template>
          <template #cell-speciesId="{ row }"><SpeciesTag :species-id="row.speciesId" /></template>
          <template #cell-lotCode="{ row }">{{ row.lotCode ?? '—' }}</template>
          <template #cell-milkAllowedFrom="{ row }">
            <span v-if="row.speciesId === 1" class="panel-muted">No aplica</span>
            <UiBadge v-else-if="row.milkAllowedFrom" tone="warning">{{ formatDate(row.milkAllowedFrom) }}</UiBadge>
            <template v-else>Sin retiro</template>
          </template>
          <template #cell-meatAllowedFrom="{ row }">
            <UiBadge v-if="row.meatAllowedFrom" tone="warning">{{ formatDate(row.meatAllowedFrom) }}</UiBadge>
            <template v-else>Sin retiro</template>
          </template>
        </UiTable>
      </UiCard>

      <UiCard title="Últimas visitas">
        <p v-if="recentVisits.length === 0" class="panel-muted">
          Aún no hay visitas.
          <RouterLink class="panel-link" :to="{ name: 'health-visits' }">Registrar la primera</RouterLink>
        </p>
        <UiTable v-else label="Últimas visitas del veterinario" :columns="visitColumns" :rows="recentVisits">
          <template #cell-visitDate="{ row }">
            <RouterLink class="panel-link" :to="{ name: 'health-visit', params: { id: row.id } }">{{ formatDate(row.visitDate) }}</RouterLink>
          </template>
          <template #cell-total="{ row }">{{ formatMoney(row.total) }}</template>
        </UiTable>
      </UiCard>
    </div>
  </UiBasePage>
</template>
