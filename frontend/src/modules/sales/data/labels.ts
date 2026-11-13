import type { DeliveryType, OrderStatus, PaymentMethod, SaleUnit } from '../types/sales'

export const statusLabels: Record<OrderStatus, string> = { confirmed: 'Encargo', delivered: 'Entregada', cancelled: 'Cancelada' }
export const statusTone = (status: OrderStatus) => (status === 'delivered' ? 'success' : status === 'confirmed' ? 'warning' : 'danger')
export const deliveryLabels: Record<DeliveryType, string> = { pickup: 'Recoge el cliente', delivery: 'A domicilio' }
export const paymentLabels: Record<PaymentMethod, string> = { cash: 'Efectivo', transfer: 'Transferencia', card: 'Tarjeta', check: 'Cheque' }
export const paymentOptions = (Object.keys(paymentLabels) as PaymentMethod[]).map((value) => ({ value, label: paymentLabels[value] }))

const numberFormat = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 3 })

export function formatQuantity(value: number, unit: SaleUnit): string {
  if (unit === 'piece') return `${numberFormat.format(value)} ${value === 1 ? 'pieza' : 'piezas'}`
  return `${numberFormat.format(value)} kg`
}

export const priceUnit = (unit: SaleUnit) => (unit === 'piece' ? 'pieza' : 'kg')
