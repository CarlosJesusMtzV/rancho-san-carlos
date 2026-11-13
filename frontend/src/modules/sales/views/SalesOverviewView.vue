<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBadge, UiBasePage, UiCard, UiChip, UiLoadError, UiMetric, UiTable, UiToast } from '../../../shared/components'
import { formatDate, formatMoney, todayIso } from '../../../shared/utils/format'
import { getSummary, listOrders } from '../api/salesApi'
import { deliveryLabels, statusLabels, statusTone } from '../data/labels'
import type { OrderStatus, OrderSummary, SalesSummary } from '../types/sales'

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const summary = ref<SalesSummary | null>(null)
const orders = ref<OrderSummary[] | null>(null)
const filter = ref<OrderStatus | 'all'>('all')
const error = ref('')

const rows = computed(() => (orders.value ?? []).filter((order) => filter.value === 'all' || order.status === filter.value))
const columns = [
  { key: 'id' as const, label: 'Pedido' },
  { key: 'orderDate' as const, label: 'Fecha' },
  { key: 'customerName' as const, label: 'Cliente' },
  { key: 'status' as const, label: 'Estado' },
  { key: 'deliveryType' as const, label: 'Entrega' },
  { key: 'total' as const, label: 'Total' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    ;[summary.value, orders.value] = await Promise.all([getSummary(), listOrders()])
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
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
  <UiBasePage fluid title="Ventas" description="Ventas en el momento y encargos de queso, ganado en pie y cabrito en canal.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn" :to="{ name: 'sale-new' }">Nueva venta</RouterLink>
        <RouterLink class="btn secondary" :to="{ name: 'customers' }">Clientes</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!summary || !orders" aria-busy="true">Cargando…</p>

    <div v-else class="stack">
      <div class="panel-metrics">
        <UiCard metric><UiMetric label="Vendido en el mes" :value="formatMoney(summary.monthTotal)" description="Entregado y cobrado" /></UiCard>
        <UiCard metric><UiMetric label="Queso" :value="formatMoney(summary.monthByKind.cheese)" description="En el mes" /></UiCard>
        <UiCard metric><UiMetric label="Ganado y canal" :value="formatMoney(summary.monthByKind.animal + summary.monthByKind.carcass)" :description="`En pie ${formatMoney(summary.monthByKind.animal)} · canal ${formatMoney(summary.monthByKind.carcass)}`" /></UiCard>
        <UiCard metric>
          <UiMetric label="Encargos pendientes" :value="summary.pendingOrders" :description="summary.dueSoon ? `${summary.dueSoon} por entregar en 2 días o vencidos` : 'Ninguno por vencer'" />
        </UiCard>
      </div>

      <UiCard title="Pedidos" description="Los encargos pendientes y los pedidos de los últimos 90 días.">
        <div class="panel-chips" role="group" aria-label="Estado">
          <UiChip :pressed="filter === 'all'" @click="filter = 'all'">Todos</UiChip>
          <UiChip :pressed="filter === 'confirmed'" @click="filter = 'confirmed'">Encargos</UiChip>
          <UiChip :pressed="filter === 'delivered'" @click="filter = 'delivered'">Entregadas</UiChip>
          <UiChip :pressed="filter === 'cancelled'" @click="filter = 'cancelled'">Canceladas</UiChip>
        </div>
        <p v-if="rows.length === 0" class="panel-muted">
          No hay pedidos con este filtro.
          <RouterLink class="panel-link" :to="{ name: 'sale-new' }">Registrar una venta</RouterLink>
        </p>
        <UiTable v-else label="Pedidos" :columns="columns" :rows="rows">
          <template #cell-id="{ row }"><RouterLink class="panel-link" :to="{ name: 'sale', params: { id: row.id } }">Pedido {{ row.id }}</RouterLink></template>
          <template #cell-orderDate="{ row }">{{ formatDate(row.orderDate) }}</template>
          <template #cell-status="{ row }">
            <UiBadge :tone="statusTone(row.status)">{{ statusLabels[row.status] }}</UiBadge>
            <span v-if="row.invoiceFolio" class="panel-muted"> · factura {{ row.invoiceFolio }}</span>
          </template>
          <template #cell-deliveryType="{ row }">
            {{ deliveryLabels[row.deliveryType] }}
            <template v-if="row.status === 'confirmed' && row.estimatedDeliveryDate">
              <UiBadge v-if="row.estimatedDeliveryDate < today" tone="danger">vencido {{ formatDate(row.estimatedDeliveryDate) }}</UiBadge>
              <span v-else class="panel-muted"> · {{ formatDate(row.estimatedDeliveryDate) }}</span>
            </template>
            <span v-else-if="row.deliveredOn" class="panel-muted"> · {{ formatDate(row.deliveredOn) }}</span>
          </template>
          <template #cell-total="{ row }"><strong>{{ formatMoney(row.total) }}</strong></template>
        </UiTable>
      </UiCard>
    </div>

    <UiToast ref="toast" />
  </UiBasePage>
</template>
