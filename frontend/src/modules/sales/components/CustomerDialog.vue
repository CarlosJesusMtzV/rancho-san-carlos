<script setup lang="ts">
import { reactive, ref, watch } from 'vue'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiButton, UiDialog, UiField } from '../../../shared/components'
import { createCustomer, updateCustomer } from '../api/salesApi'
import type { Customer } from '../types/sales'

const props = defineProps<{ customer: Customer | null; types: { id: number; name: string }[] }>()
const open = defineModel<boolean>({ default: false })
const emit = defineEmits<{ saved: [customer: Customer] }>()

const form = reactive({ name: '', customerTypeId: '', rfc: '', phone: '', email: '', address: '' })
const error = ref('')
const saving = ref(false)

watch(open, (value) => {
  if (!value) return
  const customer = props.customer
  Object.assign(form, {
    name: customer?.name ?? '',
    customerTypeId: String(customer?.customerTypeId ?? props.types[0]?.id ?? ''),
    rfc: customer?.rfc ?? '',
    phone: customer?.phone ?? '',
    email: customer?.email ?? '',
    address: customer?.address ?? '',
  })
  error.value = ''
})

async function save(): Promise<void> {
  if (saving.value) return
  error.value = form.name.trim() ? '' : 'Escribe el nombre del cliente.'
  if (error.value) return
  saving.value = true
  const input = {
    name: form.name.trim(),
    customerTypeId: Number(form.customerTypeId),
    rfc: form.rfc.trim() || null,
    phone: form.phone.trim() || null,
    email: form.email.trim() || null,
    address: form.address.trim() || null,
  }
  try {
    const saved = props.customer ? await updateCustomer(props.customer.id, input) : await createCustomer(input)
    open.value = false
    emit('saved', saved)
  } catch (cause) {
    error.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <UiDialog v-model="open" :title="customer ? `Editar ${customer.name}` : 'Nuevo cliente'">
    <form id="sl-customer-form" class="ui-form" novalidate @submit.prevent="save">
      <UiField v-model="form.name" label="Nombre o razón social *" maxlength="160" autocomplete="organization" />
      <UiField v-model="form.customerTypeId" as="select" label="Tipo *" :options="types.map((type) => ({ value: String(type.id), label: type.name }))" />
      <UiField v-model="form.phone" label="Teléfono" type="tel" maxlength="20" autocomplete="tel" />
      <UiField v-model="form.address" label="Dirección de entrega" maxlength="255" autocomplete="street-address" />
      <UiField v-model="form.rfc" label="RFC" maxlength="13" hint="Solo si pide factura." autocomplete="off" />
      <UiField v-model="form.email" label="Correo" type="email" maxlength="254" autocomplete="email" />
      <UiAlert v-if="error" tone="danger" title="No se guardó">{{ error }}</UiAlert>
    </form>
    <template #actions>
      <UiButton type="submit" form="sl-customer-form" :loading="saving">{{ customer ? 'Guardar cliente' : 'Agregar cliente' }}</UiButton>
      <UiButton variant="secondary" @click="open = false">Cancelar</UiButton>
    </template>
  </UiDialog>
</template>
