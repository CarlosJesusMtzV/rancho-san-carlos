<script setup lang="ts">
import { ref, watch } from 'vue'

import { UiAlert, UiButton, UiDialog } from '../../../shared/components'
import { formatPrice } from '../composables/useOrder'
import type { OrderLine } from '../types/catalog'

const open = defineModel<boolean>({ default: false })
const props = defineProps<{ lines: OrderLine[]; total: number; hasQuotes: boolean; summary: () => string }>()
const emit = defineEmits<{ add: [key: string]; decrease: [key: string]; remove: [key: string]; clear: [] }>()

const copyState = ref<'idle' | 'copied' | 'failed'>('idle')
const copying = ref(false)

watch(open, () => { copyState.value = 'idle' })

async function copySummary(): Promise<void> {
  if (copying.value) return
  copying.value = true
  try {
    await navigator.clipboard.writeText(props.summary())
    copyState.value = 'copied'
  } catch {
    copyState.value = 'failed'
  } finally {
    copying.value = false
  }
}
</script>

<template>
  <UiDialog v-model="open" title="Tu pedido directo del rancho" class="lp-order">
    <p v-if="lines.length === 0">Aún no has agregado quesos ni solicitudes de ganado.</p>
    <template v-else>
      <ul class="lp-order-lines">
        <li v-for="line in lines" :key="line.key">
          <div>
            <strong>{{ line.productName }}</strong>
            <span>{{ line.price === null ? `${line.presentationLabel} · Por cotizar` : `${line.presentationLabel} · ${formatPrice(line.price)}` }}</span>
          </div>
          <div class="lp-order-qty">
            <template v-if="line.price !== null">
              <button type="button" :aria-label="`Quitar una unidad de ${line.productName}`" @click="emit('decrease', line.key)">−</button>
              <span aria-live="polite">{{ line.quantity }}</span>
              <button type="button" :aria-label="`Agregar una unidad de ${line.productName}`" @click="emit('add', line.key)">+</button>
            </template>
            <button type="button" class="lp-order-remove" @click="emit('remove', line.key)">Quitar</button>
          </div>
        </li>
      </ul>
      <p class="lp-order-total"><span>Total estimado</span><strong>{{ formatPrice(total) }}</strong></p>
      <p v-if="hasQuotes" class="lp-order-hint">El ganado se cotiza por separado y no se suma al total.</p>
      <UiAlert v-if="copyState === 'copied'" tone="success" title="Resumen copiado">
        Pégalo en un mensaje para enviarlo al rancho.
      </UiAlert>
      <UiAlert v-else-if="copyState === 'failed'" tone="warning" title="No se pudo copiar">
        Tu navegador bloqueó el portapapeles. Anota el pedido y compártelo con el rancho.
      </UiAlert>
      <UiAlert v-else tone="info" title="Pedido de demostración">
        El envío en línea todavía no está conectado. Copia el resumen para mandarlo al rancho.
      </UiAlert>
    </template>
    <template #actions>
      <UiButton v-if="lines.length" :loading="copying" @click="copySummary">Copiar resumen</UiButton>
      <UiButton v-if="lines.length" variant="danger" @click="emit('clear')">Vaciar pedido</UiButton>
      <UiButton variant="secondary" @click="open = false">Cerrar</UiButton>
    </template>
  </UiDialog>
</template>
