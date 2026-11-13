<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiBasePage, UiButton, UiCard, UiField, UiLoadError, UiTable, UiToast } from '../../../shared/components'
import CustomerDialog from '../components/CustomerDialog.vue'
import { getCatalog, listCustomers } from '../api/salesApi'
import type { Customer } from '../types/sales'

const toast = ref<InstanceType<typeof UiToast> | null>(null)
const customers = ref<Customer[] | null>(null)
const types = ref<{ id: number; name: string }[]>([])
const search = ref('')
const error = ref('')
const dialogOpen = ref(false)
const editing = ref<Customer | null>(null)
let timer: ReturnType<typeof setTimeout> | undefined

const columns = [
  { key: 'name' as const, label: 'Cliente' },
  { key: 'customerTypeName' as const, label: 'Tipo' },
  { key: 'phone' as const, label: 'Teléfono' },
  { key: 'rfc' as const, label: 'RFC' },
  { key: 'address' as const, label: 'Dirección' },
  { key: 'id' as const, label: 'Acciones' },
]

async function load(): Promise<void> {
  error.value = ''
  try {
    customers.value = await listCustomers(search.value.trim())
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  }
}

function onSearch(value: string): void {
  search.value = value
  clearTimeout(timer)
  timer = setTimeout(load, 250)
}

function open(customer: Customer | null): void {
  editing.value = customer
  dialogOpen.value = true
}

async function saved(customer: Customer): Promise<void> {
  toast.value?.show(`Cliente ${customer.name} guardado.`)
  await load()
}

onMounted(async () => {
  try {
    types.value = (await getCatalog()).customerTypes
  } catch {
    // El diálogo muestra el error de la API si no hay tipos.
  }
  await load()
})
</script>

<template>
  <UiBasePage fluid title="Clientes" description="Restaurantes, tiendas, compradores de ganado y particulares.">
    <template #actions>
      <div class="panel-toolbar">
        <UiButton @click="open(null)">Nuevo cliente</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'sales' }">Volver a ventas</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="error" :message="error" @retry="load" />
    <p v-else-if="!customers" aria-busy="true">Cargando…</p>
    <UiCard v-else :title="`${customers.length} cliente${customers.length === 1 ? '' : 's'}`">
      <div class="panel-filters">
        <UiField :model-value="search" label="Buscar" type="search" placeholder="Nombre, RFC o teléfono" @update:model-value="onSearch" />
      </div>
      <p v-if="customers.length === 0" class="panel-muted">{{ search ? 'Ningún cliente coincide.' : 'Aún no hay clientes.' }}</p>
      <UiTable v-else label="Clientes" :columns="columns" :rows="customers">
        <template #cell-phone="{ row }">{{ row.phone ?? '—' }}</template>
        <template #cell-rfc="{ row }">{{ row.rfc ?? '—' }}</template>
        <template #cell-address="{ row }">{{ row.address ?? '—' }}</template>
        <template #cell-id="{ row }">
          <div class="cu-actions">
            <button type="button" class="panel-icon-button panel-icon-button--neutral" :aria-label="`Editar ${row.name}`" @click="open(row)">Editar</button>
            <RouterLink class="panel-link" :to="{ name: 'sale-new', query: { cliente: row.id } }">Vender</RouterLink>
          </div>
        </template>
      </UiTable>
    </UiCard>

    <CustomerDialog v-model="dialogOpen" :customer="editing" :types="types" @saved="saved" />
    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.cu-actions { display: flex; align-items: center; gap: 4px 16px; flex-wrap: wrap; }
.cu-actions .panel-icon-button { padding: 0; }
</style>
