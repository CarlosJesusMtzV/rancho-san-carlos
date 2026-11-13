export type LineKind = 'cheese' | 'animal' | 'carcass'
export type OrderStatus = 'confirmed' | 'delivered' | 'cancelled'
export type DeliveryType = 'pickup' | 'delivery'
export type PaymentMethod = 'cash' | 'transfer' | 'card' | 'check'
export type SaleUnit = 'kg' | 'piece' | 'head'

export interface SalesProduct {
  id: number
  sku: string
  name: string
  kind: string
  saleUnit: SaleUnit
  unitPrice: number | null
}

export interface SalesCatalog {
  products: SalesProduct[]
  customerTypes: { id: number; name: string }[]
}

export interface Customer {
  id: number
  name: string
  customerTypeId: number
  customerTypeName: string
  rfc: string | null
  phone: string | null
  email: string | null
  address: string | null
}

export interface CustomerInput {
  name: string
  customerTypeId: number
  rfc: string | null
  phone: string | null
  email: string | null
  address: string | null
}

export interface AvailableCheese {
  source: 'batch' | 'purchase'
  sourceId: number
  code: string
  productId: number
  productName: string
  saleUnit: SaleUnit
  available: number
  expiresOn: string
  unitPrice: number | null
}

export interface AvailableAnimal {
  animalId: number
  visualTag: string
  productId: number
  productName: string
  sex: 'M' | 'F'
  ageMonths: number | null
  lastWeightKg: number | null
  meatAllowedFrom: string | null
}

export interface AvailableCarcass {
  slaughterId: number
  visualTag: string
  productId: number
  carcassWeightKg: number
  runDate: string
}

export interface AvailableItems {
  cheese: AvailableCheese[]
  animals: AvailableAnimal[]
  carcasses: AvailableCarcass[]
}

export interface OrderLine {
  id: number
  kind: LineKind
  productId: number
  productName: string
  saleUnit: SaleUnit
  source: 'batch' | 'purchase' | null
  sourceId: number
  label: string
  quantity: number
  unitPrice: number
  total: number
}

export interface OrderSummary {
  id: number
  customerId: number
  customerName: string
  orderDate: string
  estimatedDeliveryDate: string | null
  status: OrderStatus
  deliveryType: DeliveryType
  deliveredOn: string | null
  itemCount: number
  total: number
  invoiceFolio: string | null
}

export interface OrderDetail extends OrderSummary {
  deliveryAddress: string | null
  notes: string | null
  items: OrderLine[]
  payment: { date: string; amount: number; method: PaymentMethod } | null
  invoice: { fiscalFolio: string; issueDate: string; subtotal: number; taxes: number; total: number } | null
}

export interface SalesSummary {
  monthTotal: number
  monthByKind: { cheese: number; animal: number; carcass: number }
  pendingOrders: number
  dueSoon: number
}

export interface OrderLineInput {
  kind: LineKind
  source?: 'batch' | 'purchase'
  sourceId: number
  quantity?: number
  unitPrice: number
}

export interface OrderInput {
  customerId: number
  orderDate: string
  estimatedDeliveryDate: string | null
  deliveryType: DeliveryType
  deliveryAddress: string | null
  notes: string | null
  items: OrderLineInput[]
  deliverNow?: { paymentMethod: PaymentMethod }
}
