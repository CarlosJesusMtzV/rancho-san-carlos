<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiToast } from '../../../shared/components'
import { formatDate, formatMoney, todayIso } from '../../../shared/utils/format'
import { getVisit, updateVisit } from '../api/healthApi'
import HealthEventsTable from '../components/HealthEventsTable.vue'
import type { VetVisitDetail } from '../types/health'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const visit = ref<VetVisitDetail | null>(null)
const error = ref('')
const today = todayIso()

const form = reactive({ visitDate: '', vetName: '', serviceFee: '', medicationCost: '', notes: '' })
const saveError = ref('')
const saving = ref(false)
const id = computed(() => Number(route.params.id))
const formTotal = computed(() => (Number(form.serviceFee) || 0) + (Number(form.medicationCost) || 0))

function fill(detail: VetVisitDetail): void {
  visit.value = detail
  Object.assign(form, {
    visitDate: detail.visitDate,
    vetName: detail.vetName,
    serviceFee: String(detail.serviceFee),
    medicationCost: String(detail.medicationCost),
    notes: detail.notes ?? '',
  })
}

async function load(): Promise<void> {
  error.value = ''
  try {
    fill(await getVisit(id.value))
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  if (!form.vetName.trim()) {
    saveError.value = 'Escribe el nombre del veterinario.'
    return
  }
  saving.value = true
  try {
    fill(await updateVisit(id.value, {
      visitDate: form.visitDate,
      vetName: form.vetName.trim(),
      serviceFee: Number(form.serviceFee) || 0,
      medicationCost: Number(form.medicationCost) || 0,
      notes: form.notes.trim() || null,
    }))
    toast.value?.show('Visita actualizada.')
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await load()
  if (!visit.value) return
  const registered = Number(route.query.registrados)
  if (route.query.nueva) toast.value?.show('Visita registrada. Ahora registra los eventos que atendió.')
  else if (registered > 0) toast.value?.show(`${registered} evento${registered === 1 ? '' : 's'} registrado${registered === 1 ? '' : 's'}.`)
  if (route.query.nueva || route.query.registrados) void router.replace({ query: {} })
})
</script>

<template>
  <UiBasePage fluid :title="visit ? `Visita del ${formatDate(visit.visitDate)}` : 'Visita del veterinario'">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'health-visits' }">Volver a visitas</RouterLink>
        <RouterLink v-if="visit" class="btn" :to="{ name: 'health-event-new', query: { visita: visit.id } }">Registrar evento en esta visita</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!visit" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <UiCard title="Datos y pago">
        <form class="panel-form" novalidate @submit.prevent="save">
          <UiField
            v-model="form.visitDate"
            label="Fecha *"
            type="date"
            :max="today"
            :disabled="visit.eventCount > 0"
            :hint="visit.eventCount > 0 ? 'No se cambia porque ya tiene eventos.' : undefined"
          />
          <UiField v-model="form.vetName" label="Veterinario *" maxlength="120" required />
          <UiField v-model="form.serviceFee" label="Honorarios ($)" type="number" min="0" step="0.01" inputmode="decimal" />
          <UiField v-model="form.medicationCost" label="Medicamentos ($)" type="number" min="0" step="0.01" inputmode="decimal" />
          <div class="panel-span"><UiField v-model="form.notes" label="Notas" maxlength="255" /></div>
          <div class="panel-span panel-actions">
            <UiButton type="submit" variant="secondary" :loading="saving">Guardar cambios</UiButton>
            <span class="panel-muted">Total: <strong>{{ formatMoney(formTotal) }}</strong></span>
          </div>
        </form>
        <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
      </UiCard>

      <UiCard title="Eventos atendidos" :description="`${visit.eventCount} evento${visit.eventCount === 1 ? '' : 's'} · ${visit.animalCount} animal${visit.animalCount === 1 ? '' : 'es'}`">
        <p v-if="visit.events.length === 0" class="panel-muted">
          Esta visita aún no tiene eventos.
          <RouterLink class="panel-link" :to="{ name: 'health-event-new', query: { visita: visit.id } }">Registrar el primero</RouterLink>
        </p>
        <HealthEventsTable v-else label="Eventos de la visita" :events="visit.events" :show-visit="false" />
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
