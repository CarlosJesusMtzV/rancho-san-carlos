<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBadge, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiMetric, UiTable, UiToast, UiToggle } from '../../../shared/components'
import { formatDate, todayIso } from '../../../shared/utils/format'
import { createAdjustment, getSummary, listStock } from '../api/cheeseApi'
import { expiryText, expiryTone, formatQuantity, reasonOptions, unitLabel } from '../data/labels'
import type { AdjustmentReason, CheeseSummary, StockItem } from '../types/cheese'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const summary = ref<CheeseSummary | null>(null)
const stock = ref<StockItem[] | null>(null)
const showAll = ref(false)
const error = ref('')

const target = ref<StockItem | null>(null)
const dialogOpen = ref(false)
const form = reactive({ reason: 'waste' as AdjustmentReason, quantity: '', date: today, notes: '' })
const saveError = ref('')
const saving = ref(false)

const rows = computed(() => (stock.value ?? []).map((item) => ({ ...item, id: `${item.source}-${item.sourceId}` })))
const columns = [
  { key: 'code' as const, label: 'Tanda o compra' },
  { key: 'productName' as const, label: 'Queso' },
  { key: 'entryDate' as const, label: 'Entró' },
  { key: 'expiresOn' as const, label: 'Caducidad' },
  { key: 'available' as const, label: 'Disponible' },
  { key: 'id' as const, label: 'Acciones' },
]

const detailRoute = (item: StockItem) =>
  item.source === 'batch' ? { name: 'cheese-batch', params: { id: item.parentId } } : { name: 'cheese-purchase', params: { id: item.parentId } }

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[summary.value, stock.value] = await Promise.all([getSummary(), listStock(showAll.value)])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function toggleAll(value: boolean): Promise<void> {
  showAll.value = value
  try {
    stock.value = await listStock(value)
  } catch (cause) {
    toast.value?.show(getApiErrorMessage(cause))
  }
}

function openAdjustment(item: StockItem): void {
  target.value = item
  Object.assign(form, { reason: 'waste', quantity: '', date: today, notes: '' })
  saveError.value = ''
  dialogOpen.value = true
}

async function saveAdjustment(): Promise<void> {
  const item = target.value
  if (!item || saving.value) return
  saveError.value = ''
  const quantity = Number(form.quantity)
  if (!(quantity > 0)) {
    saveError.value = 'Indica cuánto queso sale.'
    return
  }
  if (quantity > item.available) {
    saveError.value = `Solo hay ${formatQuantity(item.available, item.saleUnit)}.`
    return
  }
  saving.value = true
  try {
    const saved = await createAdjustment({
      source: item.source,
      sourceId: item.sourceId,
      adjustmentDate: form.date,
      reason: form.reason,
      quantity,
      notes: form.notes.trim() || null,
    })
    dialogOpen.value = false
    toast.value?.show(`Se descontaron ${formatQuantity(saved.quantity, saved.saleUnit)} de ${saved.productName} (${saved.code}).`)
    await load()
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await load()
  if (typeof route.query.guardado === 'string' && route.query.guardado) {
    toast.value?.show(route.query.guardado)
    void router.replace({ query: {} })
  }
})
</script>

<template>
  <UiBasePage fluid title="Quesería" description="Canasta y panela de leche de cabra propia; asadero de proveedor para reventa.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'cheese-batch-new' }">Registrar tanda</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'cheese-purchase-new' }">Comprar asadero</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'cheese-history' }">Historial</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!summary || !stock" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="panel-metrics">
        <UiCard v-for="product in summary.products" :key="product.productId" metric>
          <UiMetric
            :label="product.productName"
            :value="formatQuantity(product.available, product.saleUnit)"
            :description="product.sku === 'QSO-ASA' ? 'De proveedor, leche de vaca' : 'Leche de cabra propia'"
          />
        </UiCard>
        <UiCard metric>
          <UiMetric label="Por caducar" :value="summary.expiringSoon" description="Con existencia, en 2 días o menos" />
        </UiCard>
      </div>

      <UiAlert v-if="summary.expired" tone="danger" title="Queso caducado en existencia">
        {{ summary.expired }} {{ summary.expired === 1 ? 'entrada caducó' : 'entradas caducaron' }} y aún tiene{{ summary.expired === 1 ? '' : 'n' }} queso. Regístralo como merma para que no se venda.
      </UiAlert>

      <UiCard title="Existencias" :description="showAll ? 'Incluye lo agotado de los últimos 90 días.' : 'Solo lo que queda, de la caducidad más próxima a la más lejana.'">
        <div class="panel-filters">
          <UiToggle :model-value="showAll" label="Mostrar agotados" @update:model-value="toggleAll" />
        </div>
        <p v-if="rows.length === 0" class="panel-muted">
          No hay queso en existencia.
          <RouterLink class="panel-link" :to="{ name: 'cheese-batch-new' }">Registrar una tanda</RouterLink>
        </p>
        <UiTable v-else label="Existencias de queso" :columns="columns" :rows="rows">
          <template #cell-code="{ row }">
            <RouterLink class="panel-link" :to="detailRoute(row)">{{ row.code }}</RouterLink>
          </template>
          <template #cell-productName="{ row }">
            {{ row.productName }}
            <UiBadge v-if="row.source === 'purchase'" tone="purple">De proveedor</UiBadge>
          </template>
          <template #cell-entryDate="{ row }">{{ formatDate(row.entryDate) }}</template>
          <template #cell-expiresOn="{ row }">
            {{ formatDate(row.expiresOn) }}
            <UiBadge v-if="row.available > 0" :tone="expiryTone(row.daysToExpire)">{{ expiryText(row.daysToExpire) }}</UiBadge>
          </template>
          <template #cell-available="{ row }">
            <strong>{{ formatQuantity(row.available, row.saleUnit) }}</strong>
            <span class="panel-muted"> de {{ formatQuantity(row.quantityIn, row.saleUnit) }}</span>
          </template>
          <template #cell-id="{ row }">
            <button
              v-if="row.available > 0"
              type="button"
              class="panel-icon-button panel-icon-button--neutral"
              :aria-label="`Registrar merma de ${row.productName} ${row.code}`"
              @click="openAdjustment(row)"
            >
              Merma
            </button>
            <span v-else class="panel-muted">Agotado</span>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiDialog v-model="dialogOpen" :title="`Salida de ${target?.productName ?? ''}`">
      <form id="ch-adjustment-form" class="ui-form" novalidate @submit.prevent="saveAdjustment">
        <p v-if="target">{{ target.code }} · disponible: <strong>{{ formatQuantity(target.available, target.saleUnit) }}</strong></p>
        <UiField v-model="form.reason" as="select" label="Motivo *" :options="reasonOptions" />
        <UiField
          v-model="form.quantity"
          :label="`Cantidad (${target ? unitLabel(target.saleUnit) : ''}) *`"
          type="number"
          min="0"
          :max="String(target?.available ?? '')"
          :step="target?.saleUnit === 'kg' ? '0.001' : '1'"
          inputmode="decimal"
        />
        <UiField v-model="form.date" label="Fecha *" type="date" :min="target?.entryDate" :max="today" />
        <UiField v-model="form.notes" label="Notas" maxlength="255" placeholder="Ej. Se agrietó al desmoldar" />
        <UiAlert v-if="saveError" tone="danger" title="No se registró">{{ saveError }}</UiAlert>
      </form>
      <template #actions>
        <UiButton type="submit" form="ch-adjustment-form" :loading="saving">Registrar salida</UiButton>
        <UiButton variant="secondary" @click="dialogOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
