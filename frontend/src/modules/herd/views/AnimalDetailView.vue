<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import { addWeight, deleteWeight, getAnimal, registerExit } from '../api/herdApi'
import LifecycleBadge from '../components/LifecycleBadge.vue'
import SpeciesTag from '../components/SpeciesTag.vue'
import { exitOptions, formatAge, formatDate, formatKg, lifecycleLabels, originLabels, sexLabels, stageLabel, todayIso } from '../data/labels'
import type { AnimalDetail, ManualExitStatus, WeightItem } from '../types/herd'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const animal = ref<AnimalDetail | null>(null)
const loadError = ref('')
const id = computed(() => Number(route.params.id))
const today = todayIso()

const weightForm = reactive({ weighedOn: today, weightKg: '', notes: '' })
const weightError = ref('')
const savingWeight = ref(false)
const deletingWeightId = ref<number | null>(null)

const exitOpen = ref(false)
const exitForm = reactive({ status: 'sold' as ManualExitStatus, exitDate: today })
const exitError = ref('')
const savingExit = ref(false)

const isActive = computed(() => animal.value?.lifecycleStatus === 'active')
const weightColumns = [
  { key: 'weighedOn' as const, label: 'Fecha' },
  { key: 'weightKg' as const, label: 'Peso' },
  { key: 'notes' as const, label: 'Notas' },
  { key: 'id' as const, label: 'Acciones' },
]

async function load(): Promise<void> {
  loadError.value = ''
  try {
    animal.value = await getAnimal(id.value)
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

async function saveWeight(): Promise<void> {
  if (savingWeight.value || !animal.value) return
  weightError.value = ''
  const weightKg = Number(weightForm.weightKg)
  if (!weightForm.weighedOn || !weightForm.weightKg || !(weightKg > 0)) {
    weightError.value = 'Indica la fecha y un peso mayor que cero.'
    return
  }

  savingWeight.value = true
  try {
    await addWeight(animal.value.id, {
      weighedOn: weightForm.weighedOn,
      weightKg,
      notes: weightForm.notes.trim() || null,
    })
    weightForm.weightKg = ''
    weightForm.notes = ''
    await load()
    toast.value?.show('Pesaje registrado.')
  } catch (cause) {
    weightError.value = getApiErrorMessage(cause)
  } finally {
    savingWeight.value = false
  }
}

async function removeWeight(weight: WeightItem): Promise<void> {
  if (!animal.value || deletingWeightId.value !== null) return
  deletingWeightId.value = weight.id
  weightError.value = ''
  try {
    await deleteWeight(animal.value.id, weight.id)
    await load()
    toast.value?.show(`Pesaje del ${formatDate(weight.weighedOn)} eliminado.`)
  } catch (cause) {
    weightError.value = getApiErrorMessage(cause)
  } finally {
    deletingWeightId.value = null
  }
}

async function saveExit(): Promise<void> {
  if (savingExit.value || !animal.value) return
  exitError.value = ''
  if (!exitForm.exitDate) {
    exitError.value = 'Indica la fecha de salida.'
    return
  }

  savingExit.value = true
  try {
    animal.value = await registerExit(animal.value.id, { status: exitForm.status, exitDate: exitForm.exitDate })
    exitOpen.value = false
    toast.value?.show(`${animal.value.visualTag} quedó como ${lifecycleLabels[animal.value.lifecycleStatus].toLowerCase()}.`)
  } catch (cause) {
    exitError.value = getApiErrorMessage(cause)
  } finally {
    savingExit.value = false
  }
}

watch(exitOpen, (open) => {
  if (open) {
    exitError.value = ''
    exitForm.status = 'sold'
    exitForm.exitDate = today
  }
})

watch(id, load)
onMounted(async () => {
  await load()
  if (route.query.guardado && animal.value) {
    toast.value?.show(`${animal.value.visualTag} guardado.`)
    void router.replace({ query: {} })
  }
})
</script>

<template>
  <UiBasePage fluid :title="animal ? `Arete ${animal.visualTag}` : 'Ficha del animal'">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'herd' }">Volver al hato</RouterLink>
        <RouterLink v-if="animal" class="btn secondary" :to="{ name: 'herd-edit', params: { id: animal.id } }">Editar</RouterLink>
        <RouterLink v-if="animal" class="btn secondary" :to="{ path: '/panel/sanidad/historial', query: { animal: animal.id } }">Historial sanitario</RouterLink>
        <RouterLink v-if="animal && isActive" class="btn secondary" :to="{ path: '/panel/sanidad/eventos/nuevo', query: { animal: animal.id } }">Evento sanitario</RouterLink>
        <UiButton v-if="animal && isActive" variant="danger" @click="exitOpen = true">Dar de baja</UiButton>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!animal" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <UiAlert v-if="!isActive" tone="warning" :title="`${lifecycleLabels[animal.lifecycleStatus]} el ${formatDate(animal.exitDate)}`">
        Este animal ya no está activo en el hato: se conserva su historial pero no admite pesajes.
      </UiAlert>

      <UiCard title="Datos generales">
        <dl class="panel-facts">
          <div><dt>Especie</dt><dd><SpeciesTag :species-id="animal.speciesId" /></dd></div>
          <div><dt>Raza</dt><dd>{{ animal.breedName }}</dd></div>
          <div><dt>Sexo</dt><dd>{{ sexLabels[animal.sex] }}</dd></div>
          <div><dt>Etapa</dt><dd>{{ stageLabel(animal.productionStage) }}</dd></div>
          <div><dt>Estado</dt><dd><LifecycleBadge :status="animal.lifecycleStatus" /></dd></div>
          <div><dt>Arete oficial</dt><dd>{{ animal.officialTag ?? '—' }}</dd></div>
          <div><dt>Lote</dt><dd>{{ animal.lotCode ?? '—' }}</dd></div>
          <div><dt>Nacimiento</dt><dd>{{ formatDate(animal.birthDate) }}</dd></div>
          <div><dt>Edad</dt><dd>{{ formatAge(animal.ageMonths) }}</dd></div>
          <div><dt>Origen</dt><dd>{{ originLabels[animal.origin] }}</dd></div>
          <div><dt>Peso al nacer</dt><dd>{{ formatKg(animal.birthWeightKg) }}</dd></div>
          <div><dt>Último peso</dt><dd>{{ formatKg(animal.lastWeightKg) }}<span v-if="animal.lastWeighedOn" class="panel-muted"> · {{ formatDate(animal.lastWeighedOn) }}</span></dd></div>
        </dl>
      </UiCard>

      <UiCard title="Genealogía">
        <dl class="panel-facts">
          <div>
            <dt>Padre</dt>
            <dd>
              <RouterLink v-if="animal.sire" class="panel-link" :to="{ name: 'herd-animal', params: { id: animal.sire.id } }">{{ animal.sire.visualTag }}</RouterLink>
              <template v-else>Sin registrar</template>
            </dd>
          </div>
          <div>
            <dt>Madre</dt>
            <dd>
              <RouterLink v-if="animal.dam" class="panel-link" :to="{ name: 'herd-animal', params: { id: animal.dam.id } }">{{ animal.dam.visualTag }}</RouterLink>
              <template v-else>Sin registrar</template>
            </dd>
          </div>
        </dl>
        <p v-if="animal.geneticsNotes" class="ui-text">{{ animal.geneticsNotes }}</p>
      </UiCard>

      <UiCard title="Pesajes" :description="`${animal.weights.length} registro${animal.weights.length === 1 ? '' : 's'}`">
        <form v-if="isActive" class="panel-filters" novalidate @submit.prevent="saveWeight">
          <UiField v-model="weightForm.weighedOn" label="Fecha *" type="date" :max="today" :min="animal.birthDate ?? undefined" required />
          <UiField v-model="weightForm.weightKg" label="Peso (kg) *" type="number" min="0.1" max="1500" step="0.01" inputmode="decimal" required />
          <UiField v-model="weightForm.notes" label="Notas" maxlength="255" />
          <UiButton type="submit" :loading="savingWeight">Registrar pesaje</UiButton>
        </form>
        <UiAlert v-if="weightError" tone="danger" title="No se registró">{{ weightError }}</UiAlert>
        <p v-if="animal.weights.length === 0" class="panel-muted">Aún no hay pesajes.</p>
        <UiTable v-else label="Historial de pesajes" :columns="weightColumns" :rows="animal.weights">
          <template #cell-weighedOn="{ row }">{{ formatDate(row.weighedOn) }}</template>
          <template #cell-weightKg="{ row }">{{ formatKg(row.weightKg) }}</template>
          <template #cell-notes="{ row }">{{ row.notes ?? '—' }}</template>
          <template #cell-id="{ row }">
            <button
              type="button"
              class="panel-icon-button"
              :disabled="deletingWeightId !== null"
              :aria-label="`Eliminar pesaje del ${formatDate(row.weighedOn)}`"
              @click="removeWeight(row)"
            >
              Eliminar
            </button>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiDialog v-model="exitOpen" :title="`Dar de baja ${animal?.visualTag ?? ''}`">
      <form id="herd-exit-form" class="ui-form" novalidate @submit.prevent="saveExit">
        <p>El animal dejará de estar activo. Esta acción no se puede deshacer desde el panel.</p>
        <UiField v-model="exitForm.status" as="select" label="Motivo *" :options="exitOptions" />
        <UiField v-model="exitForm.exitDate" label="Fecha de salida *" type="date" :max="today" :min="animal?.birthDate ?? undefined" required />
        <UiAlert v-if="exitError" tone="danger" title="No se registró">{{ exitError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="herd-exit-form" variant="danger" :loading="savingExit">Confirmar baja</UiButton>
        <UiButton variant="secondary" @click="exitOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
