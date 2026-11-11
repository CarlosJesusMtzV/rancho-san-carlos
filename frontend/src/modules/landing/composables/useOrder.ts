import { computed, ref } from 'vue'

import type { OrderLine, Presentation, Product } from '../types/catalog'

const currency = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 })

export function formatPrice(value: number): string {
  return `${currency.format(value)} MXN`
}

// Pedido en memoria del sitio público. No envía nada: la API de pedidos aún no existe.
export function useOrder() {
  const lines = ref<OrderLine[]>([])

  const itemCount = computed(() => lines.value.reduce((sum, line) => sum + line.quantity, 0))
  const total = computed(() => lines.value.reduce((sum, line) => sum + (line.price ?? 0) * line.quantity, 0))
  const hasQuotes = computed(() => lines.value.some((line) => line.price === null))

  function add(product: Product, presentation: Presentation): void {
    const key = `${product.id}:${presentation.id}`
    const existing = lines.value.find((line) => line.key === key)
    if (existing) {
      // Una cotización de ganado se solicita una sola vez; las cantidades se acuerdan con el rancho.
      if (existing.price !== null) existing.quantity += 1
      return
    }
    lines.value.push({
      key,
      productName: product.name,
      presentationLabel: presentation.label,
      price: presentation.price,
      quantity: 1,
    })
  }

  function increase(key: string): void {
    const line = lines.value.find((item) => item.key === key)
    if (line && line.price !== null) line.quantity += 1
  }

  function decrease(key: string): void {
    const line = lines.value.find((item) => item.key === key)
    if (!line) return
    if (line.quantity > 1) line.quantity -= 1
    else remove(key)
  }

  function remove(key: string): void {
    lines.value = lines.value.filter((line) => line.key !== key)
  }

  function clear(): void {
    lines.value = []
  }

  function summary(): string {
    const rows = lines.value.map((line) =>
      line.price === null
        ? `• ${line.presentationLabel}`
        : `• ${line.quantity} × ${line.productName} (${line.presentationLabel}) — ${formatPrice(line.price * line.quantity)}`,
    )
    const footer = [`Total estimado: ${formatPrice(total.value)}`]
    if (hasQuotes.value) footer.push('El ganado se cotiza por separado.')
    return ['Pedido para Rancho San Carlos y La Cremería San Carlos', ...rows, ...footer].join('\n')
  }

  return { lines, itemCount, total, hasQuotes, add, increase, decrease, remove, clear, summary }
}
