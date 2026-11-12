<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiMetric, UiTable } from '../../../shared/components'
import { formatDate, formatMoney, todayIso } from '../../../shared/utils/format'
import { createVisit, listVisits } from '../api/healthApi'
import type { VetVisitSummary } from '../types/health'

const router = useRouter()
const visits = ref<VetVisitSummary[] | null>(null)
const error = ref('')
const today = todayIso()

const form = reactive({ visitDate: today, vetName: '', serviceFee: '', medicationCost: '', notes: '' })
const saveError = ref('')
const saving = ref(false)

const formTotal = computed(() => (Number(form.serviceFee) || 0) + (Number(form.medicationCost) || 0))
const totalPaid = computed(() => (visits.value ?? []).reduce((sum, visit) => sum + visit.total, 0))
const totalMedication = computed(() => (visits.value ?? []).reduce((sum, visit) => sum + visit.medicationCost, 0))

const columns = [
  { key: 'visitDate' as const, label: 'Fecha' },
  { key: 'vetName' as const, label: 'Veterinario' },
  { key: 'animalCount' as const, label: 'Animales' },
  { key: 'serviceFee' as const, label: 'Honorarios' },
  { key: 'medicationCost' as const, label: 'Medicamentos' },
  { key: 'total' as const, label: 'Total' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    visits.value = await listVisits()
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  if (!form.visitDate || !form.vetName.trim()) {
    saveError.value = 'Indica la fecha y el nombre del veterinario.'
    return
  }
  saving.value = true
  try {
    const visit = await createVisit({
      visitDate: form.visitDate,
      vetName: form.vetName.trim(),
      serviceFee: Number(form.serviceFee) || 0,
      medicationCost: Number(form.medicationCost) || 0,
      notes: form.notes.trim() || null,
    })
    await router.push({ name: 'health-visit', params: { id: visit.id }, query: { nueva: '1' } })
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Visitas del veterinario" description="Cada visita registra lo que se le pagó: honorarios y medicamentos que surtió.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'health' }">Volver a sanidad</RouterLink>
      </div>
    </template>

    <div class="stack">
      <UiCard title="Nueva visita">
        <form class="panel-form" novalidate @submit.prevent="save">
          <UiField v-model="form.visitDate" label="Fecha *" type="date" :max="today" required />
          <UiField v-model="form.vetName" label="Veterinario *" maxlength="120" placeholder="Ej. MVZ Juan Pérez" required />
          <UiField v-model="form.serviceFee" label="Honorarios ($)" type="number" min="0" step="0.01" inputmode="decimal" />
          <UiField v-model="form.medicationCost" label="Medicamentos ($)" type="number" min="0" step="0.01" inputmode="decimal" />
          <div class="panel-span">
            <UiField v-model="form.notes" label="Notas" maxlength="255" placeholder="Ej. Pagado en efectivo" />
          </div>
          <div class="panel-span panel-actions">
            <UiButton type="submit" :loading="saving">Registrar visita</UiButton>
            <span class="panel-muted">Total: <strong>{{ formatMoney(formTotal) }}</strong></span>
          </div>
        </form>
        <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
      </UiCard>

      <UiLoadError v-if="error" :message="error" @retry="load" />
      <p v-else-if="!visits" aria-busy="true">Cargando…</p>
      <template v-else>
        <div class="panel-metrics">
          <UiCard metric><UiMetric label="Pagado al veterinario" :value="formatMoney(totalPaid)" :description="`${visits.length} visita${visits.length === 1 ? '' : 's'}`" /></UiCard>
          <UiCard metric><UiMetric label="De eso, medicamentos" :value="formatMoney(totalMedication)" /></UiCard>
        </div>
        <UiCard title="Visitas">
          <p v-if="visits.length === 0" class="panel-muted">Aún no hay visitas registradas.</p>
          <UiTable v-else label="Visitas del veterinario" :columns="columns" :rows="visits">
            <template #cell-visitDate="{ row }">
              <RouterLink class="panel-link" :to="{ name: 'health-visit', params: { id: row.id } }">{{ formatDate(row.visitDate) }}</RouterLink>
            </template>
            <template #cell-serviceFee="{ row }">{{ formatMoney(row.serviceFee) }}</template>
            <template #cell-medicationCost="{ row }">{{ formatMoney(row.medicationCost) }}</template>
            <template #cell-total="{ row }"><strong>{{ formatMoney(row.total) }}</strong></template>
          </UiTable>
        </UiCard>
      </template>
    </div>
  </UiBasePage>
</template>
