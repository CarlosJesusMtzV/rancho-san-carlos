<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBasePage, UiCard, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import { formatDate, formatMoney } from '../../../shared/utils/format'
import { deleteAdjustment, listAdjustments, listBatches, listPurchases } from '../api/cheeseApi'
import { formatQuantity, reasonLabels } from '../data/labels'
import type { CheeseBatch, CheesePurchase, StockAdjustment } from '../types/cheese'

const toast = ref<InstanceType<typeof UiToast> | null>(null)
const batches = ref<CheeseBatch[] | null>(null)
const purchases = ref<CheesePurchase[] | null>(null)
const adjustments = ref<StockAdjustment[] | null>(null)
const error = ref('')
const deletingId = ref<number | null>(null)

const batchColumns = [
  { key: 'code' as const, label: 'Tanda' },
  { key: 'madeOn' as const, label: 'Elaboración' },
  { key: 'collectionDate' as const, label: 'Ordeña' },
  { key: 'litersUsed' as const, label: 'Leche' },
  { key: 'outputs' as const, label: 'Salió' },
  { key: 'expiresOn' as const, label: 'Caducidad' },
]
const purchaseColumns = [
  { key: 'code' as const, label: 'Compra' },
  { key: 'purchaseDate' as const, label: 'Fecha' },
  { key: 'supplierName' as const, label: 'Proveedor' },
  { key: 'quantity' as const, label: 'Piezas' },
  { key: 'unitCost' as const, label: 'Costo' },
  { key: 'total' as const, label: 'Total' },
]
const adjustmentColumns = [
  { key: 'adjustmentDate' as const, label: 'Fecha' },
  { key: 'code' as const, label: 'Queso' },
  { key: 'reason' as const, label: 'Motivo' },
  { key: 'quantity' as const, label: 'Cantidad' },
  { key: 'notes' as const, label: 'Notas' },
  { key: 'id' as const, label: 'Acciones' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[batches.value, purchases.value, adjustments.value] = await Promise.all([listBatches(), listPurchases(), listAdjustments()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

async function remove(item: StockAdjustment): Promise<void> {
  if (deletingId.value !== null) return
  deletingId.value = item.id
  try {
    await deleteAdjustment(item.id)
    toast.value?.show(`Salida de ${formatQuantity(item.quantity, item.saleUnit)} de ${item.productName} eliminada; vuelve a la existencia.`)
    await load()
  } catch (cause) {
    toast.value?.show(getApiErrorMessage(cause))
  } finally {
    deletingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Historial de quesería" description="Tandas, compras de asadero y salidas que no son venta, de los últimos 90 días.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'cheese' }">Volver a quesería</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!batches || !purchases || !adjustments" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <UiCard title="Tandas" :description="`${batches.length} registrada${batches.length === 1 ? '' : 's'}`">
        <p v-if="batches.length === 0" class="panel-muted">Sin tandas registradas.</p>
        <UiTable v-else label="Tandas de queso" :columns="batchColumns" :rows="batches">
          <template #cell-code="{ row }"><RouterLink class="panel-link" :to="{ name: 'cheese-batch', params: { id: row.id } }">{{ row.code }}</RouterLink></template>
          <template #cell-madeOn="{ row }">{{ formatDate(row.madeOn) }}</template>
          <template #cell-collectionDate="{ row }">
            <RouterLink class="panel-link" :to="{ name: 'milking-day', params: { id: row.milkLotId } }">{{ formatDate(row.collectionDate) }}</RouterLink>
          </template>
          <template #cell-litersUsed="{ row }">{{ row.litersUsed }} L</template>
          <template #cell-outputs="{ row }">{{ row.outputs.map((output) => `${formatQuantity(output.quantity, output.saleUnit)} de ${output.productName.toLowerCase()}`).join(', ') }}</template>
          <template #cell-expiresOn="{ row }">{{ formatDate(row.expiresOn) }}</template>
        </UiTable>
      </UiCard>

      <UiCard title="Compras de asadero" :description="`${purchases.length} registrada${purchases.length === 1 ? '' : 's'} · ${formatMoney(purchases.reduce((sum, item) => sum + item.total, 0))}`">
        <p v-if="purchases.length === 0" class="panel-muted">Sin compras registradas.</p>
        <UiTable v-else label="Compras de queso de reventa" :columns="purchaseColumns" :rows="purchases">
          <template #cell-code="{ row }"><RouterLink class="panel-link" :to="{ name: 'cheese-purchase', params: { id: row.id } }">{{ row.code }}</RouterLink></template>
          <template #cell-purchaseDate="{ row }">{{ formatDate(row.purchaseDate) }}</template>
          <template #cell-quantity="{ row }">{{ row.quantity }}</template>
          <template #cell-unitCost="{ row }">{{ formatMoney(row.unitCost) }}</template>
          <template #cell-total="{ row }"><strong>{{ formatMoney(row.total) }}</strong></template>
        </UiTable>
      </UiCard>

      <UiCard title="Mermas, consumo propio y regalos" :description="`${adjustments.length} registrada${adjustments.length === 1 ? '' : 's'}`">
        <p v-if="adjustments.length === 0" class="panel-muted">Sin salidas registradas.</p>
        <UiTable v-else label="Salidas de queso que no son venta" :columns="adjustmentColumns" :rows="adjustments">
          <template #cell-adjustmentDate="{ row }">{{ formatDate(row.adjustmentDate) }}</template>
          <template #cell-code="{ row }">{{ row.productName }} <span class="panel-muted">· {{ row.code }}</span></template>
          <template #cell-reason="{ row }">{{ reasonLabels[row.reason] }}</template>
          <template #cell-quantity="{ row }">{{ formatQuantity(row.quantity, row.saleUnit) }}</template>
          <template #cell-notes="{ row }">{{ row.notes ?? '—' }}</template>
          <template #cell-id="{ row }">
            <button
              type="button"
              class="panel-icon-button"
              :disabled="deletingId !== null"
              :aria-label="`Eliminar salida de ${row.productName} del ${formatDate(row.adjustmentDate)}`"
              @click="remove(row)"
            >
              Eliminar
            </button>
          </template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
