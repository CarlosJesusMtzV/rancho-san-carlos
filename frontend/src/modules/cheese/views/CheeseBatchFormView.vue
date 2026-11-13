<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiToast } from '../../../shared/components'
import { addDays, formatDate, todayIso } from '../../../shared/utils/format'
import { createBatch, deleteBatch, getBatch, getCatalog, listMilkLots, updateBatch } from '../api/cheeseApi'
import { batchMaxShelfLifeDays, batchShelfLifeDays, formatQuantity, unitLabel } from '../data/labels'
import type { AvailableMilkLot, CheeseBatch, CheeseProduct } from '../types/cheese'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const editId = computed(() => (route.params.id ? Number(route.params.id) : null))
const batch = ref<CheeseBatch | null>(null)
const products = ref<CheeseProduct[]>([])
const lots = ref<AvailableMilkLot[]>([])
const loadError = ref('')
const ready = ref(false)

const form = reactive({ milkLotId: '', madeOn: '', liters: '', expiresOn: '', notes: '', quantities: {} as Record<number, string> })
const saveError = ref('')
const saving = ref(false)
const deleteOpen = ref(false)
const deleting = ref(false)
const deleteError = ref('')

const lot = computed(() => {
  if (batch.value) return { id: batch.value.milkLotId, code: batch.value.milkLotCode, collectionDate: batch.value.collectionDate, totalLiters: batch.value.milkLotLiters }
  return lots.value.find((item) => String(item.id) === form.milkLotId) ?? null
})
const lotOptions = computed(() => [
  { value: '', label: 'Selecciona la ordeña' },
  ...lots.value.map((item) => ({
    value: String(item.id),
    label: `${formatDate(item.collectionDate)} · ${item.totalLiters} L · ${item.goatCount} ${item.goatCount === 1 ? 'cabra' : 'cabras'}`,
  })),
])
const maxExpires = computed(() => (form.madeOn ? addDays(form.madeOn, batchMaxShelfLifeDays) : ''))
const consumed = (productId: number) => batch.value?.outputs.find((output) => output.productId === productId)?.consumed ?? 0

// Al elegir la ordeña se sugieren su fecha y sus litros.
watch(() => form.milkLotId, () => {
  const selected = lots.value.find((item) => String(item.id) === form.milkLotId)
  if (!selected || batch.value) return
  form.madeOn = selected.collectionDate
  form.liters = String(selected.totalLiters)
})

// La caducidad sigue a la elaboración (7 días) mientras no se haya cambiado a mano.
watch(() => form.madeOn, (madeOn, previous) => {
  if (!madeOn) return
  if (!form.expiresOn || (previous && form.expiresOn === addDays(previous, batchShelfLifeDays))) {
    form.expiresOn = addDays(madeOn, batchShelfLifeDays)
  }
})

function fill(detail: CheeseBatch): void {
  batch.value = detail
  Object.assign(form, { milkLotId: String(detail.milkLotId), madeOn: detail.madeOn, liters: String(detail.litersUsed), expiresOn: detail.expiresOn, notes: detail.notes ?? '' })
  form.quantities = Object.fromEntries(products.value.map((product) => {
    const output = detail.outputs.find((item) => item.productId === product.id)
    return [product.id, output ? String(output.quantity) : '']
  }))
}

async function load(): Promise<void> {
  loadError.value = ''
  try {
    const catalog = await getCatalog()
    products.value = catalog.ownCheeses
    form.quantities = Object.fromEntries(catalog.ownCheeses.map((product) => [product.id, '']))
    if (editId.value) {
      fill(await getBatch(editId.value))
    } else {
      lots.value = await listMilkLots()
      const preselected = lots.value.find((item) => item.id === Number(route.query.ordena)) ?? (lots.value.length === 1 ? lots.value[0] : null)
      if (preselected) form.milkLotId = String(preselected.id)
    }
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

function validate(): string {
  if (!lot.value) return 'Elige la ordeña de la que sale la tanda.'
  if (!form.madeOn) return 'Indica la fecha de elaboración.'
  const liters = Number(form.liters)
  if (!(liters > 0)) return 'Indica los litros de leche usados.'
  if (liters > lot.value.totalLiters) return `La ordeña juntó ${lot.value.totalLiters} L; la tanda no puede usar más.`
  if (!form.expiresOn || form.expiresOn <= form.madeOn || form.expiresOn > maxExpires.value) {
    return `La caducidad va del día siguiente a la elaboración hasta el ${formatDate(maxExpires.value)}.`
  }
  const outputs = products.value.filter((product) => form.quantities[product.id])
  if (outputs.length === 0) return 'Indica lo que salió: kg de canasta o piezas de panela.'
  for (const product of outputs) {
    const value = Number(form.quantities[product.id])
    if (!(value > 0)) return `La cantidad de ${product.name} debe ser mayor que 0.`
    if (product.saleUnit === 'piece' && !Number.isInteger(value)) return `${product.name} se cuenta en piezas enteras.`
  }
  return ''
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = validate()
  if (saveError.value) return
  saving.value = true
  const input = {
    madeOn: form.madeOn,
    expiresOn: form.expiresOn,
    litersUsed: Number(form.liters),
    outputs: products.value
      .filter((product) => form.quantities[product.id])
      .map((product) => ({ productId: product.id, quantity: Number(form.quantities[product.id]) })),
    notes: form.notes.trim() || null,
  }
  try {
    if (batch.value) {
      fill(await updateBatch(batch.value.id, input))
      toast.value?.show('Tanda actualizada.')
    } else {
      const saved = await createBatch({ ...input, milkLotId: Number(form.milkLotId) })
      const made = saved.outputs.map((output) => formatQuantity(output.quantity, output.saleUnit)).join(' y ')
      await router.push({ name: 'cheese', query: { guardado: `Tanda ${saved.code} registrada: ${made}.` } })
    }
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (!batch.value || deleting.value) return
  deleting.value = true
  deleteError.value = ''
  try {
    await deleteBatch(batch.value.id)
    await router.push({ name: 'cheese', query: { guardado: `Tanda ${batch.value.code} eliminada; la ordeña del ${formatDate(batch.value.collectionDate)} queda libre.` } })
  } catch (cause) {
    deleteError.value = getApiErrorMessage(cause)
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage
    fluid
    :title="batch ? `Tanda ${batch.code}` : 'Registrar tanda'"
    description="Canasta y panela salen del mismo queso; solo cambia el molde. Cada tanda usa la leche de un solo día."
  >
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'cheese' }">Volver a quesería</RouterLink>
        <UiButton v-if="batch" variant="danger" @click="deleteOpen = true">Eliminar</UiButton>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <UiAlert v-else-if="!batch && lots.length === 0" tone="info" title="No hay ordeñas disponibles">
      Todas las ordeñas de los últimos 30 días ya tienen tanda.
      <RouterLink class="panel-link" to="/panel/ordena">Registrar la ordeña</RouterLink>
    </UiAlert>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>

      <UiCard title="Leche">
        <div class="panel-form">
          <UiField v-if="!batch" v-model="form.milkLotId" as="select" label="Ordeña *" :options="lotOptions" />
          <UiField v-model="form.madeOn" label="Fecha de elaboración *" type="date" :min="lot?.collectionDate" :max="today" required />
          <UiField v-model="form.liters" label="Litros usados *" type="number" min="0.01" :max="String(lot?.totalLiters ?? '')" step="0.01" inputmode="decimal" required />
          <UiField
            v-model="form.expiresOn"
            label="Caducidad *"
            type="date"
            :min="form.madeOn ? addDays(form.madeOn, 1) : undefined"
            :max="maxExpires"
            required
          />
        </div>
        <p class="panel-muted">
          Canasta y panela duran de {{ batchShelfLifeDays }} a {{ batchMaxShelfLifeDays }} días.
          <template v-if="lot">Ordeña del {{ formatDate(lot.collectionDate) }} ({{ lot.code }}): {{ lot.totalLiters }} L.</template>
          <RouterLink v-if="batch && lot" class="panel-link" :to="{ name: 'milking-day', params: { id: lot.id } }">Ver ordeña</RouterLink>
        </p>
      </UiCard>

      <UiCard title="Lo que salió" description="Llena solo las presentaciones que se hicieron.">
        <div class="panel-form">
          <UiField
            v-for="product in products"
            :key="product.id"
            v-model="form.quantities[product.id]"
            :label="`${product.name} (${unitLabel(product.saleUnit)})`"
            type="number"
            min="0"
            :step="product.saleUnit === 'kg' ? '0.001' : '1'"
            inputmode="decimal"
            :hint="consumed(product.id) > 0 ? `Ya salieron ${formatQuantity(consumed(product.id), product.saleUnit)}: no puede ser menos.` : undefined"
          />
          <UiField v-model="form.notes" label="Notas" maxlength="255" />
        </div>
      </UiCard>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">{{ batch ? 'Guardar cambios' : 'Registrar tanda' }}</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'cheese' }">Cancelar</RouterLink>
      </div>
    </form>

    <UiDialog v-model="deleteOpen" title="Eliminar tanda">
      <p v-if="batch">Se eliminará la tanda {{ batch.code }}. La ordeña del {{ formatDate(batch.collectionDate) }} quedará libre para otra tanda.</p>
      <UiAlert v-if="deleteError" tone="danger" title="No se eliminó">{{ deleteError }}</UiAlert>
      <template #actions>
        <UiButton variant="danger" :loading="deleting" @click="confirmDelete">Eliminar tanda</UiButton>
        <UiButton variant="secondary" @click="deleteOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
