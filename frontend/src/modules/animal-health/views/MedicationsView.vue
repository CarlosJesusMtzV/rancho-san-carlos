<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import { createMedication, getHealthCatalog, updateMedication } from '../api/healthApi'
import type { HealthCatalog, MedicationItem } from '../types/health'

const catalog = ref<HealthCatalog | null>(null)
const error = ref('')
const toast = ref<InstanceType<typeof UiToast> | null>(null)

const emptyForm = () => ({ supplyTypeId: '1', commercialName: '', activeIngredient: '', milkDays: '0', meatDays: '0' })
const form = reactive(emptyForm())
const editingId = ref<number | null>(null)
const saveError = ref('')
const saving = ref(false)

const typeOptions = computed(() => (catalog.value?.supplyTypes ?? []).map((type) => ({ value: String(type.id), label: type.name })))
const columns = [
  { key: 'commercialName' as const, label: 'Medicamento' },
  { key: 'supplyTypeName' as const, label: 'Tipo' },
  { key: 'activeIngredient' as const, label: 'Principio activo' },
  { key: 'defaultMilkWithdrawalDays' as const, label: 'Retiro leche' },
  { key: 'defaultMeatWithdrawalDays' as const, label: 'Retiro carne' },
  { key: 'id' as const, label: 'Acciones' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    catalog.value = await getHealthCatalog()
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

function edit(medication: MedicationItem): void {
  editingId.value = medication.id
  saveError.value = ''
  Object.assign(form, {
    supplyTypeId: String(medication.supplyTypeId),
    commercialName: medication.commercialName,
    activeIngredient: medication.activeIngredient ?? '',
    milkDays: String(medication.defaultMilkWithdrawalDays),
    meatDays: String(medication.defaultMeatWithdrawalDays),
  })
  document.getElementById('medication-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function cancelEdit(): void {
  editingId.value = null
  saveError.value = ''
  Object.assign(form, emptyForm())
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  const milk = Number(form.milkDays)
  const meat = Number(form.meatDays)
  if (!form.commercialName.trim()) {
    saveError.value = 'Escribe el nombre comercial.'
    return
  }
  if (![milk, meat].every((days) => Number.isInteger(days) && days >= 0 && days <= 365)) {
    saveError.value = 'Los días de retiro van de 0 a 365.'
    return
  }
  saving.value = true
  try {
    const input = {
      supplyTypeId: Number(form.supplyTypeId),
      commercialName: form.commercialName.trim(),
      activeIngredient: form.activeIngredient.trim() || null,
      defaultMilkWithdrawalDays: milk,
      defaultMeatWithdrawalDays: meat,
    }
    const saved = editingId.value ? await updateMedication(editingId.value, input) : await createMedication(input)
    toast.value?.show(`${saved.commercialName} ${editingId.value ? 'actualizado' : 'registrado'}.`)
    cancelEdit()
    await load()
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Medicamentos" description="Lo que aplica el veterinario o la familia. No se llevan existencias: el veterinario los surte y los cobra en la visita.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'health' }">Volver a sanidad</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!catalog" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <UiCard id="medication-form" :title="editingId ? 'Editar medicamento' : 'Nuevo medicamento'" description="Los días de retiro se proponen al aplicarlo y se pueden cambiar en cada evento.">
        <form class="panel-form" novalidate @submit.prevent="save">
          <UiField v-model="form.supplyTypeId" as="select" label="Tipo *" :options="typeOptions" />
          <UiField v-model="form.commercialName" label="Nombre comercial *" maxlength="120" required />
          <UiField v-model="form.activeIngredient" label="Principio activo" maxlength="120" />
          <UiField v-model="form.milkDays" label="Retiro de leche (días)" type="number" min="0" max="365" step="1" inputmode="numeric" />
          <UiField v-model="form.meatDays" label="Retiro de carne (días)" type="number" min="0" max="365" step="1" inputmode="numeric" />
          <div class="panel-span panel-actions">
            <UiButton type="submit" :loading="saving">{{ editingId ? 'Guardar cambios' : 'Agregar medicamento' }}</UiButton>
            <UiButton v-if="editingId" variant="secondary" @click="cancelEdit">Cancelar</UiButton>
          </div>
        </form>
        <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
      </UiCard>

      <UiCard title="Catálogo">
        <p v-if="catalog.medications.length === 0" class="panel-muted">Aún no hay medicamentos.</p>
        <UiTable v-else label="Medicamentos" :columns="columns" :rows="catalog.medications">
          <template #cell-activeIngredient="{ row }">{{ row.activeIngredient ?? '—' }}</template>
          <template #cell-defaultMilkWithdrawalDays="{ row }">{{ row.defaultMilkWithdrawalDays }} días</template>
          <template #cell-defaultMeatWithdrawalDays="{ row }">{{ row.defaultMeatWithdrawalDays }} días</template>
          <template #cell-id="{ row }">
            <button type="button" class="panel-icon-button panel-icon-button--neutral" :aria-label="`Editar ${row.commercialName}`" @click="edit(row)">Editar</button>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
