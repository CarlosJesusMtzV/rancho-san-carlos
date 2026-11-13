<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiToast } from '../../../shared/components'
import { formatDate } from '../../../shared/utils/format'
import { deleteDay, getDay, listGoats, updateDay } from '../api/milkingApi'
import GoatChecklist from '../components/GoatChecklist.vue'
import type { MilkDayDetail, MilkingGoat } from '../types/milking'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const id = computed(() => Number(route.params.id))

const day = ref<MilkDayDetail | null>(null)
const goats = ref<MilkingGoat[]>([])
const error = ref('')
const form = reactive({ liters: '', goatIds: [] as number[], notes: '' })
const saveError = ref('')
const saving = ref(false)
const deleteOpen = ref(false)
const deleting = ref(false)
const deleteError = ref('')

function fill(detail: MilkDayDetail): void {
  day.value = detail
  form.liters = String(detail.totalLiters)
  form.goatIds = detail.goats.map((goat) => goat.animalId)
  form.notes = detail.notes ?? ''
}

async function load(): Promise<void> {
  error.value = ''
  try {
    const detail = await getDay(id.value)
    const available = await listGoats(detail.collectionDate)
    // Las cabras ya ordeñadas se muestran aunque hoy tengan retiro o ya no estén activas.
    const extra = detail.goats
      .filter((goat) => !available.some((item) => item.animalId === goat.animalId))
      .map((goat) => ({ animalId: goat.animalId, visualTag: goat.visualTag, productionStage: null, lotCode: null, canBeMilked: true, milkAllowedFrom: null }))
    goats.value = [...available, ...extra].sort((a, b) => a.visualTag.localeCompare(b.visualTag))
    fill(detail)
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  const total = Number(form.liters)
  if (!(total > 0)) {
    saveError.value = 'Indica el total de litros.'
    return
  }
  if (form.goatIds.length === 0) {
    saveError.value = 'Marca al menos una cabra ordeñada.'
    return
  }
  saving.value = true
  try {
    fill(await updateDay(id.value, { totalLiters: total, goatIds: form.goatIds, notes: form.notes.trim() || null }))
    toast.value?.show('Ordeña actualizada.')
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (deleting.value || !day.value) return
  deleting.value = true
  deleteError.value = ''
  try {
    await deleteDay(id.value)
    await router.push({ name: 'milking', query: { guardado: `Ordeña del ${formatDate(day.value.collectionDate)} eliminada.` } })
  } catch (cause) {
    deleteError.value = getApiErrorMessage(cause)
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid :title="day ? `Ordeña del ${formatDate(day.collectionDate)}` : 'Ordeña del día'" :description="day ? `Lote de leche ${day.code}` : undefined">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'milking' }">Volver a ordeña</RouterLink>
        <UiButton v-if="day" variant="danger" @click="deleteOpen = true">Eliminar</UiButton>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!day" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <UiAlert v-if="day.cheeseBatch" tone="info" title="Leche usada en quesería">
        La tanda <RouterLink class="panel-link" :to="{ name: 'cheese-batch', params: { id: day.cheeseBatch.id } }">{{ day.cheeseBatch.code }}</RouterLink>
        usó {{ day.cheeseBatch.litersUsed }} L de esta ordeña: el total no puede ser menor y la ordeña no se elimina mientras exista la tanda.
      </UiAlert>
      <p v-else><RouterLink class="panel-link" :to="{ name: 'cheese-batch-new', query: { ordena: day.id } }">Hacer tanda de queso con esta leche</RouterLink></p>

      <UiCard title="Datos de la ordeña" description="La fecha no cambia; si fue otro día, elimínala y regístrala de nuevo.">
        <form class="stack" novalidate @submit.prevent="save">
          <div class="panel-form">
            <UiField v-model="form.liters" label="Total de litros *" type="number" min="0.01" max="2000" step="0.01" inputmode="decimal" required />
            <UiField v-model="form.notes" label="Notas" maxlength="255" />
          </div>
          <GoatChecklist v-model="form.goatIds" :goats="goats" :date="day.collectionDate" />
          <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
          <div class="panel-actions"><UiButton type="submit" :loading="saving">Guardar cambios</UiButton></div>
        </form>
      </UiCard>
    </div>

    <UiDialog v-model="deleteOpen" title="Eliminar ordeña">
      <p v-if="day">Se eliminará la ordeña del {{ formatDate(day.collectionDate) }} ({{ day.totalLiters }} L). El día quedará libre para registrarla de nuevo.</p>
      <UiAlert v-if="deleteError" tone="danger" title="No se eliminó">{{ deleteError }}</UiAlert>
      <template #actions>
        <UiButton variant="danger" :loading="deleting" @click="confirmDelete">Eliminar ordeña</UiButton>
        <UiButton variant="secondary" @click="deleteOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
