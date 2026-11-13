<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBadge, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiRadio, UiTable, UiToast } from '../../../shared/components'
import { formatDate, formatMoney, todayIso } from '../../../shared/utils/format'
import CustomerDialog from '../components/CustomerDialog.vue'
import { cancelOrder, createOrder, deleteInvoice, deliverOrder, getCatalog, getOrder, listAvailable, listCustomers, saveInvoice, updateOrder } from '../api/salesApi'
import { deliveryLabels, formatQuantity, paymentLabels, paymentOptions, priceUnit, statusLabels, statusTone } from '../data/labels'
import type { AvailableItems, Customer, LineKind, OrderDetail, PaymentMethod, SalesCatalog, SaleUnit } from '../types/sales'

interface LineRow {
  key: number
  kind: LineKind
  ref: string
  quantity: string
  unitPrice: string
}

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const editId = computed(() => (route.params.id ? Number(route.params.id) : null))
const order = ref<OrderDetail | null>(null)
const catalog = ref<SalesCatalog | null>(null)
const customers = ref<Customer[]>([])
const available = ref<AvailableItems>({ cheese: [], animals: [], carcasses: [] })
const loadError = ref('')
const ready = ref(false)

const form = reactive({
  customerId: '',
  orderDate: today,
  mode: 'now' as 'now' | 'later',
  estimatedDeliveryDate: '',
  deliveryType: 'pickup' as 'pickup' | 'delivery',
  deliveryAddress: '',
  notes: '',
  paymentMethod: 'cash' as PaymentMethod,
})
const lines = ref<LineRow[]>([])
let nextKey = 1
const saveError = ref('')
const saving = ref(false)
const customerOpen = ref(false)

const deliverOpen = ref(false)
const deliverForm = reactive({ deliveredOn: today, paymentMethod: 'cash' as PaymentMethod })
const cancelOpen = ref(false)
const actionError = ref('')
const acting = ref(false)
const invoiceForm = reactive({ fiscalFolio: '', issueDate: today, taxes: '' })
const invoiceError = ref('')

const editable = computed(() => !order.value || order.value.status === 'confirmed')
const customer = computed(() => customers.value.find((item) => String(item.id) === form.customerId) ?? null)
const customerOptions = computed(() => [
  { value: '', label: customers.value.length ? 'Selecciona el cliente' : 'Agrega un cliente' },
  ...customers.value.map((item) => ({ value: String(item.id), label: `${item.name} · ${item.customerTypeName}` })),
])
const kindTitles: Record<LineKind, string> = { cheese: 'Queso', animal: 'Ganado en pie', carcass: 'Cabrito en canal' }

const animalAge = (months: number | null) => (months === null ? '' : months === 1 ? ', 1 mes' : `, ${months} meses`)
function options(row: LineRow): { value: string; label: string }[] {
  const used = new Set(lines.value.filter((line) => line.key !== row.key).map((line) => line.ref))
  const list =
    row.kind === 'cheese'
      ? available.value.cheese.map((item) => ({
          value: `${item.source}:${item.sourceId}`,
          label: `${item.productName} · ${item.code} · ${formatQuantity(item.available, item.saleUnit)} · caduca ${formatDate(item.expiresOn)}`,
        }))
      : row.kind === 'animal'
        ? available.value.animals
            .filter((item) => !item.meatAllowedFrom)
            .map((item) => ({
              value: `animal:${item.animalId}`,
              label: `${item.visualTag} · ${item.productName}${animalAge(item.ageMonths)}${item.lastWeightKg !== null ? `, último peso ${item.lastWeightKg} kg` : ''}`,
            }))
        : available.value.carcasses.map((item) => ({
            value: `carcass:${item.slaughterId}`,
            label: `Canal de ${item.visualTag} · ${item.carcassWeightKg} kg · matanza del ${formatDate(item.runDate)}`,
          }))
  return [{ value: '', label: `Selecciona ${row.kind === 'cheese' ? 'el queso' : row.kind === 'animal' ? 'el animal' : 'la canal'}` }, ...list.filter((item) => !used.has(item.value))]
}
const withdrawn = computed(() => available.value.animals.filter((item) => item.meatAllowedFrom))

function cheeseOf(row: LineRow) {
  return available.value.cheese.find((item) => `${item.source}:${item.sourceId}` === row.ref) ?? null
}
function carcassOf(row: LineRow) {
  return available.value.carcasses.find((item) => `carcass:${item.slaughterId}` === row.ref) ?? null
}
function animalOf(row: LineRow) {
  return available.value.animals.find((item) => `animal:${item.animalId}` === row.ref) ?? null
}
function unitOf(row: LineRow): SaleUnit {
  return row.kind === 'cheese' ? (cheeseOf(row)?.saleUnit ?? 'kg') : 'kg'
}
function quantityOf(row: LineRow): number {
  return row.kind === 'carcass' ? (carcassOf(row)?.carcassWeightKg ?? 0) : Number(row.quantity) || 0
}
const lineTotal = (row: LineRow) => Math.round(quantityOf(row) * (Number(row.unitPrice) || 0) * 100) / 100
const total = computed(() => lines.value.reduce((sum, row) => sum + lineTotal(row), 0))

function addLine(kind: LineKind): void {
  lines.value.push({ key: nextKey++, kind, ref: '', quantity: '', unitPrice: '' })
}
function removeLine(key: number): void {
  lines.value = lines.value.filter((row) => row.key !== key)
}
// Al elegir un queso se sugiere su precio de catálogo.
function pick(row: LineRow, value: string): void {
  row.ref = value
  const cheese = cheeseOf(row)
  if (cheese && !row.unitPrice && cheese.unitPrice !== null) row.unitPrice = String(cheese.unitPrice)
}

async function loadAvailable(): Promise<void> {
  try {
    available.value = await listAvailable(form.orderDate, order.value?.id)
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  }
}

function fill(detail: OrderDetail): void {
  order.value = detail
  Object.assign(form, {
    customerId: String(detail.customerId),
    orderDate: detail.orderDate,
    mode: 'later',
    estimatedDeliveryDate: detail.estimatedDeliveryDate ?? '',
    deliveryType: detail.deliveryType,
    deliveryAddress: detail.deliveryAddress ?? '',
    notes: detail.notes ?? '',
  })
  lines.value = detail.items.map((item) => ({
    key: nextKey++,
    kind: item.kind,
    ref: item.kind === 'cheese' ? `${item.source}:${item.sourceId}` : `${item.kind}:${item.sourceId}`,
    quantity: String(item.quantity),
    unitPrice: String(item.unitPrice),
  }))
  if (detail.invoice) Object.assign(invoiceForm, { fiscalFolio: detail.invoice.fiscalFolio, issueDate: detail.invoice.issueDate, taxes: String(detail.invoice.taxes) })
}

async function load(): Promise<void> {
  loadError.value = ''
  try {
    ;[catalog.value, customers.value] = await Promise.all([getCatalog(), listCustomers()])
    if (editId.value) fill(await getOrder(editId.value))
    else if (customers.value.some((item) => item.id === Number(route.query.cliente))) form.customerId = String(route.query.cliente)
    if (editable.value) await loadAvailable()
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

function validate(): string {
  if (!form.customerId) return 'Elige el cliente.'
  if (!form.orderDate) return 'Indica la fecha.'
  if (form.mode === 'later' && form.estimatedDeliveryDate && form.estimatedDeliveryDate < form.orderDate) return 'La entrega estimada no puede ser anterior al pedido.'
  if (form.deliveryType === 'delivery' && !form.deliveryAddress.trim()) return 'Escribe la dirección de entrega.'
  if (lines.value.length === 0) return 'Agrega al menos un producto.'
  for (const [index, row] of lines.value.entries()) {
    const position = `Renglón ${index + 1}`
    if (!row.ref) return `${position}: elige qué se vende.`
    if (row.kind !== 'carcass' && !(Number(row.quantity) > 0)) return `${position}: indica la cantidad${row.kind === 'animal' ? ' en kg en pie' : ''}.`
    const cheese = cheeseOf(row)
    if (cheese && Number(row.quantity) > cheese.available) return `${position}: solo hay ${formatQuantity(cheese.available, cheese.saleUnit)} de ${cheese.productName}.`
    if (cheese?.saleUnit === 'piece' && !Number.isInteger(Number(row.quantity))) return `${position}: ${cheese.productName} se vende en piezas enteras.`
    if (row.unitPrice === '' || !(Number(row.unitPrice) >= 0)) return `${position}: indica el precio.`
  }
  return ''
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = validate()
  if (saveError.value) return
  saving.value = true
  const input = {
    customerId: Number(form.customerId),
    orderDate: form.orderDate,
    estimatedDeliveryDate: form.mode === 'later' && form.estimatedDeliveryDate ? form.estimatedDeliveryDate : null,
    deliveryType: form.deliveryType,
    deliveryAddress: form.deliveryType === 'delivery' ? form.deliveryAddress.trim() : null,
    notes: form.notes.trim() || null,
    items: lines.value.map((row) => {
      const [source, id] = row.ref.split(':')
      return {
        kind: row.kind,
        source: row.kind === 'cheese' ? (source as 'batch' | 'purchase') : undefined,
        sourceId: Number(id),
        quantity: row.kind === 'carcass' ? undefined : Number(row.quantity),
        unitPrice: Number(row.unitPrice),
      }
    }),
    deliverNow: !order.value && form.mode === 'now' ? { paymentMethod: form.paymentMethod } : undefined,
  }
  try {
    if (order.value) {
      fill(await updateOrder(order.value.id, input))
      await loadAvailable()
      toast.value?.show('Encargo actualizado.')
    } else {
      const saved = await createOrder(input)
      const what = saved.status === 'delivered' ? `Venta registrada y cobrada: ${formatMoney(saved.total)}.` : `Encargo registrado: ${formatMoney(saved.total)}.`
      await router.push({ name: 'sales', query: { guardado: `${what} Pedido ${saved.id}, ${saved.customerName}.` } })
    }
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

async function runAction(action: () => Promise<OrderDetail>, message: string, close: () => void): Promise<void> {
  if (acting.value) return
  acting.value = true
  actionError.value = ''
  try {
    fill(await action())
    close()
    toast.value?.show(message)
  } catch (cause) {
    actionError.value = getApiErrorMessage(cause)
  } finally {
    acting.value = false
  }
}

const confirmDeliver = () =>
  runAction(() => deliverOrder(order.value!.id, { ...deliverForm }), 'Encargo entregado y cobrado.', () => (deliverOpen.value = false))
const confirmCancel = () =>
  runAction(() => cancelOrder(order.value!.id), order.value?.status === 'delivered' ? 'Venta cancelada; los animales vuelven al hato.' : 'Encargo cancelado; lo apartado queda libre.', () => (cancelOpen.value = false))

async function submitInvoice(): Promise<void> {
  if (!order.value || acting.value) return
  invoiceError.value = invoiceForm.fiscalFolio.trim() ? '' : 'Escribe el folio fiscal.'
  if (invoiceError.value) return
  acting.value = true
  try {
    fill(await saveInvoice(order.value.id, { fiscalFolio: invoiceForm.fiscalFolio.trim(), issueDate: invoiceForm.issueDate, taxes: Number(invoiceForm.taxes) || 0 }))
    toast.value?.show('Factura guardada.')
  } catch (cause) {
    invoiceError.value = getApiErrorMessage(cause)
  } finally {
    acting.value = false
  }
}

async function removeInvoice(): Promise<void> {
  if (!order.value || acting.value) return
  acting.value = true
  try {
    fill(await deleteInvoice(order.value.id))
    Object.assign(invoiceForm, { fiscalFolio: '', issueDate: today, taxes: '' })
    toast.value?.show('Factura quitada.')
  } catch (cause) {
    invoiceError.value = getApiErrorMessage(cause)
  } finally {
    acting.value = false
  }
}

function customerSaved(saved: Customer): void {
  customers.value = [...customers.value.filter((item) => item.id !== saved.id), saved].sort((a, b) => a.name.localeCompare(b.name))
  form.customerId = String(saved.id)
  toast.value?.show(`Cliente ${saved.name} guardado.`)
}

// La dirección de entrega se toma del cliente mientras no se haya escrito otra.
watch([() => form.customerId, () => form.deliveryType], () => {
  if (form.deliveryType === 'delivery' && !form.deliveryAddress && customer.value?.address) form.deliveryAddress = customer.value.address
})
watch(() => form.orderDate, () => {
  if (ready.value && editable.value && form.orderDate) void loadAvailable()
})

onMounted(load)
</script>

<template>
  <UiBasePage fluid :title="order ? `Pedido ${order.id}` : 'Nueva venta'" :description="order ? `${order.customerName} · ${formatDate(order.orderDate)}` : 'Venta en el momento o encargo para entregar después.'">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'sales' }">Volver a ventas</RouterLink>
        <UiButton v-if="order?.status === 'confirmed'" @click="deliverOpen = true; actionError = ''">Entregar y cobrar</UiButton>
        <UiButton v-if="order && order.status !== 'cancelled'" variant="danger" @click="cancelOpen = true; actionError = ''">
          {{ order.status === 'delivered' ? 'Cancelar venta' : 'Cancelar encargo' }}
        </UiButton>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready || !catalog" aria-busy="true">Cargando…</p>

    <!-- Venta entregada o cancelada: solo lectura -->
    <div v-else-if="order && !editable" class="stack">
      <UiCard title="Pedido">
        <div class="panel-facts">
          <p><span class="panel-muted">Estado</span> <UiBadge :tone="statusTone(order.status)">{{ statusLabels[order.status] }}</UiBadge></p>
          <p><span class="panel-muted">Cliente</span> {{ order.customerName }}</p>
          <p><span class="panel-muted">Entrega</span> {{ deliveryLabels[order.deliveryType] }}<template v-if="order.deliveryAddress">: {{ order.deliveryAddress }}</template></p>
          <p v-if="order.deliveredOn"><span class="panel-muted">Entregado</span> {{ formatDate(order.deliveredOn) }}</p>
          <p v-if="order.payment"><span class="panel-muted">Pago</span> {{ formatMoney(order.payment.amount) }} · {{ paymentLabels[order.payment.method] }}</p>
          <p v-if="order.notes"><span class="panel-muted">Notas</span> {{ order.notes }}</p>
        </div>
      </UiCard>
      <UiCard title="Productos" :description="`Total ${formatMoney(order.total)}`">
        <UiTable
          label="Productos del pedido"
          :columns="[{ key: 'label', label: 'Producto' }, { key: 'quantity', label: 'Cantidad' }, { key: 'unitPrice', label: 'Precio' }, { key: 'total', label: 'Importe' }]"
          :rows="order.items"
        >
          <template #cell-quantity="{ row }">{{ formatQuantity(row.quantity, row.saleUnit) }}</template>
          <template #cell-unitPrice="{ row }">{{ formatMoney(row.unitPrice) }} / {{ priceUnit(row.saleUnit) }}</template>
          <template #cell-total="{ row }"><strong>{{ formatMoney(row.total) }}</strong></template>
        </UiTable>
      </UiCard>
      <UiCard v-if="order.status === 'delivered'" title="Factura" description="Se anota el folio de la factura que emitiste; el sistema no timbra.">
        <div v-if="order.invoice" class="stack">
          <p>Folio <strong>{{ order.invoice.fiscalFolio }}</strong> del {{ formatDate(order.invoice.issueDate) }} · subtotal {{ formatMoney(order.invoice.subtotal) }}, impuestos {{ formatMoney(order.invoice.taxes) }}, total <strong>{{ formatMoney(order.invoice.total) }}</strong>.</p>
          <div class="panel-actions"><UiButton variant="secondary" :loading="acting" @click="removeInvoice">Quitar factura</UiButton></div>
        </div>
        <form v-else class="stack" novalidate @submit.prevent="submitInvoice">
          <div class="panel-form">
            <UiField v-model="invoiceForm.fiscalFolio" label="Folio fiscal *" maxlength="40" autocomplete="off" />
            <UiField v-model="invoiceForm.issueDate" label="Fecha *" type="date" :min="order.orderDate" :max="today" />
            <UiField v-model="invoiceForm.taxes" label="Impuestos (MXN)" type="number" min="0" step="0.01" inputmode="decimal" hint="Queso y ganado van a tasa 0." />
          </div>
          <UiAlert v-if="invoiceError" tone="danger" title="No se guardó">{{ invoiceError }}</UiAlert>
          <div class="panel-actions"><UiButton type="submit" :loading="acting">Guardar factura</UiButton></div>
        </form>
      </UiCard>
    </div>

    <!-- Nueva venta o encargo editable -->
    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>
      <UiAlert v-if="order" tone="info" title="Encargo pendiente">
        Aparta lo que lleva.<template v-if="order.estimatedDeliveryDate"> Entrega estimada: {{ formatDate(order.estimatedDeliveryDate) }}.</template> Al entregar, usa «Entregar y cobrar».
      </UiAlert>

      <UiCard title="Cliente y entrega">
        <div class="panel-form">
          <div class="sl-inline">
            <UiField v-model="form.customerId" as="select" label="Cliente *" :options="customerOptions" />
            <UiButton variant="secondary" @click="customerOpen = true">Nuevo cliente</UiButton>
          </div>
          <UiField v-model="form.orderDate" label="Fecha *" type="date" :max="today" />
          <fieldset v-if="!order" class="panel-radios panel-span">
            <legend>Tipo de venta *</legend>
            <UiRadio v-model="form.mode" name="mode" value="now" label="Venta en el momento (se entrega y cobra hoy)" />
            <UiRadio v-model="form.mode" name="mode" value="later" label="Encargo (se entrega y cobra después)" />
          </fieldset>
          <UiField v-if="form.mode === 'later'" v-model="form.estimatedDeliveryDate" label="Entrega estimada" type="date" :min="form.orderDate" />
          <fieldset class="panel-radios panel-span">
            <legend>Entrega *</legend>
            <UiRadio v-model="form.deliveryType" name="delivery" value="pickup" label="Recoge el cliente" />
            <UiRadio v-model="form.deliveryType" name="delivery" value="delivery" label="Entregar a domicilio (queso)" />
          </fieldset>
          <UiField v-if="form.deliveryType === 'delivery'" v-model="form.deliveryAddress" label="Dirección de entrega *" maxlength="255" />
          <UiField v-model="form.notes" label="Notas" maxlength="255" />
        </div>
      </UiCard>

      <UiCard title="Productos" description="Queso por kg o pieza; ganado en pie y canal por kilo.">
        <p v-if="lines.length === 0" class="panel-muted">Agrega lo que se vende.</p>
        <div v-for="(row, index) in lines" :key="row.key" class="sl-line">
          <UiField
            :model-value="row.ref"
            as="select"
            :label="`${kindTitles[row.kind]} (renglón ${index + 1}) *`"
            :options="options(row)"
            @update:model-value="pick(row, $event)"
          />
          <UiField
            v-if="row.kind !== 'carcass'"
            v-model="row.quantity"
            :label="row.kind === 'animal' ? `Kg en pie (renglón ${index + 1}) *` : `Cantidad en ${unitOf(row) === 'piece' ? 'piezas' : 'kg'} (renglón ${index + 1}) *`"
            type="number"
            min="0"
            :step="unitOf(row) === 'piece' ? '1' : '0.01'"
            inputmode="decimal"
            :placeholder="animalOf(row)?.lastWeightKg ? `Último: ${animalOf(row)?.lastWeightKg}` : undefined"
          />
          <p v-else class="sl-fixed"><span class="panel-muted">Kg de canal</span> {{ carcassOf(row)?.carcassWeightKg ?? '—' }}</p>
          <UiField v-model="row.unitPrice" :label="`Precio por ${priceUnit(unitOf(row))} (renglón ${index + 1}) *`" type="number" min="0" step="0.01" inputmode="decimal" />
          <p class="sl-fixed"><span class="panel-muted">Importe</span> <strong>{{ formatMoney(lineTotal(row)) }}</strong></p>
          <button type="button" class="panel-icon-button" :aria-label="`Quitar el renglón ${index + 1}`" @click="removeLine(row.key)">Quitar</button>
        </div>
        <div class="panel-actions">
          <UiButton variant="secondary" @click="addLine('cheese')">Agregar queso</UiButton>
          <UiButton variant="secondary" @click="addLine('animal')">Agregar ganado en pie</UiButton>
          <UiButton variant="secondary" @click="addLine('carcass')">Agregar canal</UiButton>
        </div>
        <p v-if="withdrawn.length" class="panel-muted">Con retiro de carne, no se venden: {{ withdrawn.map((item) => `${item.visualTag} (hasta el ${formatDate(item.meatAllowedFrom)})`).join(', ') }}.</p>
        <p class="sl-total">Total <strong>{{ formatMoney(total) }}</strong></p>
      </UiCard>

      <UiCard v-if="!order && form.mode === 'now'" title="Cobro">
        <div class="panel-form">
          <UiField v-model="form.paymentMethod" as="select" label="Forma de pago *" :options="paymentOptions" />
        </div>
      </UiCard>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">{{ order ? 'Guardar cambios' : form.mode === 'now' ? 'Registrar venta' : 'Registrar encargo' }}</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'sales' }">Cancelar</RouterLink>
      </div>
    </form>

    <CustomerDialog v-if="catalog" v-model="customerOpen" :customer="null" :types="catalog.customerTypes" @saved="customerSaved" />

    <UiDialog v-model="deliverOpen" title="Entregar y cobrar">
      <form id="sl-deliver-form" class="ui-form" novalidate @submit.prevent="confirmDeliver">
        <p v-if="order">Se cobra el total: <strong>{{ formatMoney(order.total) }}</strong>. Los animales en pie salen del hato como vendidos.</p>
        <UiField v-model="deliverForm.deliveredOn" label="Fecha de entrega *" type="date" :min="order?.orderDate" :max="today" />
        <UiField v-model="deliverForm.paymentMethod" as="select" label="Forma de pago *" :options="paymentOptions" />
        <UiAlert v-if="actionError" tone="danger" title="No se entregó">{{ actionError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="sl-deliver-form" :loading="acting">Entregar y cobrar</UiButton>
        <UiButton variant="secondary" @click="deliverOpen = false">Volver</UiButton>
      </template>
    </UiDialog>

    <UiDialog v-model="cancelOpen" :title="order?.status === 'delivered' ? 'Cancelar venta' : 'Cancelar encargo'">
      <p v-if="order?.status === 'delivered'">Se anula el cobro de {{ formatMoney(order.total) }}, el queso vuelve a existencia y los animales vuelven al hato.</p>
      <p v-else>Lo apartado queda libre para otros clientes. Un pedido cancelado no se reactiva.</p>
      <UiAlert v-if="actionError" tone="danger" title="No se canceló">{{ actionError }}</UiAlert>
      <template #actions>
        <UiButton variant="danger" :loading="acting" @click="confirmCancel">Sí, cancelar</UiButton>
        <UiButton variant="secondary" @click="cancelOpen = false">Volver</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.sl-inline { display: flex; flex-wrap: wrap; align-items: end; gap: 12px; min-width: 0; }
.sl-inline > :first-child { flex: 1 1 200px; min-width: 0; }
.sl-inline :deep(.field) { margin-bottom: 0; }
@media (min-width: 900px) {
  .sl-inline { grid-column: span 2; }
}
.sl-line {
  display: grid;
  grid-template-columns: minmax(0, 2.4fr) minmax(0, 1fr) minmax(0, 1fr) minmax(0, 0.9fr) auto;
  gap: 12px;
  align-items: end;
  padding-bottom: 12px;
  margin-bottom: 12px;
  border-bottom: 1px solid var(--color-border-light);
}
.sl-line :deep(.field) { margin-bottom: 0; }
.sl-fixed { margin: 0 0 12px; }
.sl-total { margin: 16px 0 0; text-align: right; font-size: 18px; }
@media (max-width: 900px) {
  .sl-line { grid-template-columns: 1fr 1fr; }
  .sl-line > :first-child { grid-column: span 2; }
}
</style>
