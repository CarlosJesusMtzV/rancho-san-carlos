<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiToast } from '../../../shared/components'
import { addDays, formatDate, formatMoney, todayIso } from '../../../shared/utils/format'
import { createPurchase, createSupplier, deletePurchase, getCatalog, getPurchase, updatePurchase } from '../api/cheeseApi'
import { formatQuantity, purchaseShelfLifeDays } from '../data/labels'
import type { CheeseProduct, CheesePurchase, CheeseSupplier } from '../types/cheese'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const editId = computed(() => (route.params.id ? Number(route.params.id) : null))
const purchase = ref<CheesePurchase | null>(null)
const products = ref<CheeseProduct[]>([])
const suppliers = ref<CheeseSupplier[]>([])
const loadError = ref('')
const ready = ref(false)

const form = reactive({ supplierId: '', productId: '', purchaseDate: today, quantity: '', unitCost: '', expiresOn: addDays(today, purchaseShelfLifeDays), notes: '' })
const saveError = ref('')
const saving = ref(false)

const supplierOpen = ref(false)
const supplierForm = reactive({ name: '', phone: '' })
const supplierError = ref('')
const savingSupplier = ref(false)

const deleteOpen = ref(false)
const deleting = ref(false)
const deleteError = ref('')

const product = computed(() => products.value.find((item) => String(item.id) === form.productId) ?? null)
const supplierOptions = computed(() => [
  { value: '', label: suppliers.value.length ? 'Selecciona el proveedor' : 'Agrega un proveedor' },
  ...suppliers.value.map((supplier) => ({ value: String(supplier.id), label: supplier.phone ? `${supplier.name} · ${supplier.phone}` : supplier.name })),
])
const productOptions = computed(() => products.value.map((item) => ({ value: String(item.id), label: item.name })))
const total = computed(() => {
  const quantity = Number(form.quantity)
  const cost = Number(form.unitCost)
  return quantity > 0 && form.unitCost !== '' && cost >= 0 ? Math.round(quantity * cost * 100) / 100 : null
})

// La caducidad sigue a la fecha de compra (15 días) mientras no se haya cambiado a mano.
watch(() => form.purchaseDate, (date, previous) => {
  if (date && previous && form.expiresOn === addDays(previous, purchaseShelfLifeDays)) {
    form.expiresOn = addDays(date, purchaseShelfLifeDays)
  }
})

function fill(detail: CheesePurchase): void {
  purchase.value = detail
  Object.assign(form, {
    supplierId: String(detail.supplierId),
    productId: String(detail.productId),
    purchaseDate: detail.purchaseDate,
    quantity: String(detail.quantity),
    unitCost: String(detail.unitCost),
    expiresOn: detail.expiresOn,
    notes: detail.notes ?? '',
  })
}

async function load(): Promise<void> {
  loadError.value = ''
  try {
    const catalog = await getCatalog()
    products.value = catalog.resaleCheeses
    suppliers.value = catalog.suppliers
    if (editId.value) fill(await getPurchase(editId.value))
    else if (products.value.length) form.productId = String(products.value[0].id)
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

function validate(): string {
  if (!form.supplierId) return 'Elige el proveedor.'
  if (!form.productId) return 'Elige el queso comprado.'
  if (!form.purchaseDate) return 'Indica la fecha de compra.'
  const quantity = Number(form.quantity)
  if (!(quantity > 0) || !Number.isInteger(quantity)) return 'Las piezas compradas deben ser un número entero mayor que 0.'
  if (form.unitCost === '' || !(Number(form.unitCost) >= 0)) return 'Indica el costo por pieza.'
  if (!form.expiresOn || form.expiresOn <= form.purchaseDate) return 'La caducidad debe ser posterior a la fecha de compra.'
  if (purchase.value && quantity < purchase.value.consumed) return `Ya salieron ${formatQuantity(purchase.value.consumed, 'piece')}: no pueden ser menos piezas.`
  return ''
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = validate()
  if (saveError.value) return
  saving.value = true
  const input = {
    supplierId: Number(form.supplierId),
    productId: Number(form.productId),
    purchaseDate: form.purchaseDate,
    expiresOn: form.expiresOn,
    quantity: Number(form.quantity),
    unitCost: Number(form.unitCost),
    notes: form.notes.trim() || null,
  }
  try {
    if (purchase.value) {
      fill(await updatePurchase(purchase.value.id, input))
      toast.value?.show('Compra actualizada.')
    } else {
      const saved = await createPurchase(input)
      await router.push({
        name: 'cheese',
        query: { guardado: `Compra ${saved.code} registrada: ${formatQuantity(saved.quantity, 'piece')} de ${saved.productName} por ${formatMoney(saved.total)}.` },
      })
    }
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

function openSupplier(): void {
  Object.assign(supplierForm, { name: '', phone: '' })
  supplierError.value = ''
  supplierOpen.value = true
}

async function saveSupplier(): Promise<void> {
  if (savingSupplier.value) return
  supplierError.value = supplierForm.name.trim() ? '' : 'Escribe el nombre del proveedor.'
  if (supplierError.value) return
  savingSupplier.value = true
  try {
    const saved = await createSupplier({ name: supplierForm.name.trim(), phone: supplierForm.phone.trim() || null })
    suppliers.value = [...suppliers.value, saved].sort((a, b) => a.name.localeCompare(b.name))
    form.supplierId = String(saved.id)
    supplierOpen.value = false
    toast.value?.show(`Proveedor ${saved.name} agregado.`)
  } catch (cause) {
    supplierError.value = getApiErrorMessage(cause)
  } finally {
    savingSupplier.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (!purchase.value || deleting.value) return
  deleting.value = true
  deleteError.value = ''
  try {
    await deletePurchase(purchase.value.id)
    await router.push({ name: 'cheese', query: { guardado: `Compra ${purchase.value.code} eliminada.` } })
  } catch (cause) {
    deleteError.value = getApiErrorMessage(cause)
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid :title="purchase ? `Compra ${purchase.code}` : 'Comprar asadero'" description="Queso asadero de leche de vaca comprado a un proveedor para reventa.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'cheese' }">Volver a quesería</RouterLink>
        <UiButton v-if="purchase" variant="danger" @click="deleteOpen = true">Eliminar</UiButton>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>

      <UiCard title="Compra">
        <div class="panel-form">
          <div class="ch-supplier">
            <UiField v-model="form.supplierId" as="select" label="Proveedor *" :options="supplierOptions" />
            <UiButton variant="secondary" @click="openSupplier">Nuevo proveedor</UiButton>
          </div>
          <UiField v-if="products.length > 1 && !purchase" v-model="form.productId" as="select" label="Queso *" :options="productOptions" />
          <UiField v-model="form.purchaseDate" label="Fecha de compra *" type="date" :max="today" required />
          <UiField v-model="form.quantity" label="Piezas *" type="number" min="1" step="1" inputmode="numeric" required
            :hint="purchase && purchase.consumed > 0 ? `Ya salieron ${formatQuantity(purchase.consumed, 'piece')}.` : undefined" />
          <UiField v-model="form.unitCost" label="Costo por pieza (MXN) *" type="number" min="0" step="0.01" inputmode="decimal" required />
          <UiField v-model="form.expiresOn" label="Caducidad *" type="date" :min="addDays(form.purchaseDate || today, 1)" required />
          <UiField v-model="form.notes" label="Notas" maxlength="255" />
        </div>
        <p class="panel-muted">
          {{ product?.name ?? 'Queso de reventa' }}: no se elabora aquí ni tiene trazabilidad a la ordeña. Caducidad sugerida: {{ purchaseShelfLifeDays }} días.
          <template v-if="total !== null"> Total de la compra: <strong>{{ formatMoney(total) }}</strong>.</template>
        </p>
      </UiCard>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">{{ purchase ? 'Guardar cambios' : 'Registrar compra' }}</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'cheese' }">Cancelar</RouterLink>
      </div>
    </form>

    <UiDialog v-model="supplierOpen" title="Nuevo proveedor">
      <form id="ch-supplier-form" class="ui-form" novalidate @submit.prevent="saveSupplier">
        <UiField v-model="supplierForm.name" label="Nombre *" maxlength="160" autocomplete="organization" />
        <UiField v-model="supplierForm.phone" label="Teléfono" type="tel" maxlength="20" autocomplete="tel" />
        <UiAlert v-if="supplierError" tone="danger" title="No se agregó">{{ supplierError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="ch-supplier-form" :loading="savingSupplier">Agregar proveedor</UiButton>
        <UiButton variant="secondary" @click="supplierOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiDialog v-model="deleteOpen" title="Eliminar compra">
      <p v-if="purchase">Se eliminará la compra {{ purchase.code }} del {{ formatDate(purchase.purchaseDate) }} ({{ formatQuantity(purchase.quantity, 'piece') }}).</p>
      <UiAlert v-if="deleteError" tone="danger" title="No se eliminó">{{ deleteError }}</UiAlert>
      <template #actions>
        <UiButton variant="danger" :loading="deleting" @click="confirmDelete">Eliminar compra</UiButton>
        <UiButton variant="secondary" @click="deleteOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.ch-supplier { display: flex; flex-wrap: wrap; align-items: end; gap: 12px; min-width: 0; }
.ch-supplier > :first-child { flex: 1 1 200px; min-width: 0; }
.ch-supplier :deep(.field) { margin-bottom: 0; }
.ch-supplier .btn { flex: none; }
</style>
