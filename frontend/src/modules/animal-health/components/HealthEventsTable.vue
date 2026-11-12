<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { UiBadge, UiTable } from '../../../shared/components'
import { formatDate } from '../../../shared/utils/format'
import { eventTypeLabels } from '../data/labels'
import type { HealthEventItem } from '../types/health'

withDefaults(defineProps<{ events: HealthEventItem[]; label: string; showVisit?: boolean; deletable?: boolean; busyId?: number | null }>(), {
  showVisit: true,
  deletable: false,
  busyId: null,
})
const emit = defineEmits<{ remove: [event: HealthEventItem] }>()

const columns = [
  { key: 'eventDate' as const, label: 'Fecha' },
  { key: 'visualTag' as const, label: 'Arete' },
  { key: 'eventType' as const, label: 'Evento' },
  { key: 'treatments' as const, label: 'Medicamentos' },
  { key: 'milkAllowedFrom' as const, label: 'Leche a partir de' },
  { key: 'meatAllowedFrom' as const, label: 'Carne a partir de' },
  { key: 'vetName' as const, label: 'Visita' },
  { key: 'id' as const, label: 'Acciones' },
]
</script>

<template>
  <UiTable
    :label="label"
    :columns="columns.filter((column) => (showVisit || column.key !== 'vetName') && (deletable || column.key !== 'id'))"
    :rows="events"
  >
    <template #cell-eventDate="{ row }">{{ formatDate(row.eventDate) }}</template>
    <template #cell-visualTag="{ row }">
      <RouterLink class="panel-link" :to="`/panel/hato/${row.animalId}`">{{ row.visualTag }}</RouterLink>
    </template>
    <template #cell-eventType="{ row }">
      {{ eventTypeLabels[row.eventType] }}<span v-if="row.diagnosis" class="panel-muted"> · {{ row.diagnosis }}</span>
    </template>
    <template #cell-treatments="{ row }">
      <template v-if="row.treatments.length">
        <span v-for="(treatment, index) in row.treatments" :key="treatment.id">
          {{ index ? ', ' : '' }}{{ treatment.medicationName }} ({{ treatment.dose }})
        </span>
      </template>
      <span v-else class="panel-muted">—</span>
    </template>
    <template #cell-milkAllowedFrom="{ row }">
      <span v-if="row.speciesId === 1" class="panel-muted">No aplica</span>
      <UiBadge v-else-if="row.milkAllowedFrom" tone="warning">{{ formatDate(row.milkAllowedFrom) }}</UiBadge>
      <template v-else>—</template>
    </template>
    <template #cell-meatAllowedFrom="{ row }">
      <UiBadge v-if="row.meatAllowedFrom" tone="warning">{{ formatDate(row.meatAllowedFrom) }}</UiBadge>
      <template v-else>—</template>
    </template>
    <template #cell-vetName="{ row }">
      <RouterLink v-if="row.vetVisitId" class="panel-link" :to="{ name: 'health-visit', params: { id: row.vetVisitId } }">{{ row.vetName }}</RouterLink>
      <span v-else class="panel-muted">Familia</span>
    </template>
    <template #cell-id="{ row }">
      <button
        type="button"
        class="panel-icon-button"
        :disabled="busyId !== null"
        :aria-label="`Eliminar ${eventTypeLabels[row.eventType].toLowerCase()} de ${row.visualTag} del ${formatDate(row.eventDate)}`"
        @click="emit('remove', row)"
      >
        Eliminar
      </button>
    </template>
  </UiTable>
</template>
