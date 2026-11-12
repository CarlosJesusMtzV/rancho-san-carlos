<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBadge, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import { formatDate, todayIso } from '../../../shared/utils/format'
import { listAnimals, type AnimalSummary } from '../../herd'
import { createLabTest, listLabTests, setLabResult } from '../api/healthApi'
import { labResultLabels, labResultOptions, labResultTone } from '../data/labels'
import type { LabResult, LabTestItem } from '../types/health'
import '../styles.css'

const tests = ref<LabTestItem[] | null>(null)
const animals = ref<AnimalSummary[]>([])
const error = ref('')
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const form = reactive({ animalId: '', sampleDate: today, testType: '', labName: '' })
const saveError = ref('')
const saving = ref(false)
const updatingId = ref<number | null>(null)

const animalOptions = computed(() => [
  { value: '', label: 'Selecciona un animal' },
  ...animals.value.map((animal) => ({ value: String(animal.id), label: `${animal.visualTag} · ${animal.speciesName}` })),
])
const columns = [
  { key: 'sampleDate' as const, label: 'Muestra' },
  { key: 'visualTag' as const, label: 'Arete' },
  { key: 'testType' as const, label: 'Prueba' },
  { key: 'labName' as const, label: 'Laboratorio' },
  { key: 'result' as const, label: 'Resultado' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    const [testList, animalPage] = await Promise.all([listLabTests(), listAnimals({ status: 'active', pageSize: 200 })])
    tests.value = testList
    animals.value = animalPage.items
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  if (!form.animalId || !form.sampleDate || !form.testType.trim()) {
    saveError.value = 'Elige el animal e indica la fecha y el tipo de prueba.'
    return
  }
  saving.value = true
  try {
    const test = await createLabTest({
      animalId: Number(form.animalId),
      sampleDate: form.sampleDate,
      testType: form.testType.trim(),
      labName: form.labName.trim() || null,
    })
    tests.value = [test, ...(tests.value ?? [])]
    form.animalId = ''
    form.testType = ''
    toast.value?.show(`Muestra de ${test.visualTag} registrada como pendiente.`)
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

async function changeResult(test: LabTestItem, result: string): Promise<void> {
  if (updatingId.value !== null || result === test.result) return
  updatingId.value = test.id
  try {
    const updated = await setLabResult(test.id, result as LabResult)
    tests.value = (tests.value ?? []).map((item) => (item.id === updated.id ? updated : item))
    toast.value?.show(`Resultado de ${updated.visualTag}: ${labResultLabels[updated.result].toLowerCase()}.`)
  } catch (cause) {
    toast.value?.show(getApiErrorMessage(cause))
  } finally {
    updatingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Pruebas de laboratorio" description="Muestras enviadas (brucelosis, tuberculosis, mastitis…) y sus resultados.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'health' }">Volver a sanidad</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!tests" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <UiCard title="Nueva muestra">
        <form class="panel-form" novalidate @submit.prevent="save">
          <UiField v-model="form.animalId" as="select" label="Animal *" :options="animalOptions" />
          <UiField v-model="form.sampleDate" label="Fecha de muestra *" type="date" :max="today" required />
          <UiField v-model="form.testType" label="Prueba *" maxlength="40" placeholder="Ej. Brucelosis" required />
          <UiField v-model="form.labName" label="Laboratorio" maxlength="120" />
          <div class="panel-span"><UiButton type="submit" :loading="saving">Registrar muestra</UiButton></div>
        </form>
        <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
      </UiCard>

      <UiCard title="Pruebas" :description="`${tests.filter((test) => test.result === 'pending').length} pendiente(s) de resultado`">
        <p v-if="tests.length === 0" class="panel-muted">Aún no hay pruebas registradas.</p>
        <UiTable v-else label="Pruebas de laboratorio" :columns="columns" :rows="tests">
          <template #cell-sampleDate="{ row }">{{ formatDate(row.sampleDate) }}</template>
          <template #cell-visualTag="{ row }">
            <RouterLink class="panel-link" :to="`/panel/hato/${row.animalId}`">{{ row.visualTag }}</RouterLink>
          </template>
          <template #cell-labName="{ row }">{{ row.labName ?? '—' }}</template>
          <template #cell-result="{ row }">
            <div class="panel-actions hl-result">
              <UiBadge :tone="labResultTone(row.result)">{{ labResultLabels[row.result] }}</UiBadge>
              <select class="hl-result-select" :aria-label="`Cambiar resultado de ${row.visualTag}`" :value="row.result" :disabled="updatingId !== null" @change="changeResult(row, ($event.target as HTMLSelectElement).value)">
                <option v-for="option in labResultOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
              </select>
            </div>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
