export type SaleUnit = 'kg' | 'piece'
export type StockSource = 'batch' | 'purchase'
export type AdjustmentReason = 'waste' | 'own_use' | 'gift'

export interface CheeseProduct {
  id: number
  sku: string
  name: string
  saleUnit: SaleUnit
  unitPrice: number | null
}

export interface CheeseSupplier {
  id: number
  name: string
  phone: string | null
}

export interface CheeseCatalog {
  ownCheeses: CheeseProduct[]
  resaleCheeses: CheeseProduct[]
  suppliers: CheeseSupplier[]
}

export interface AvailableMilkLot {
  id: number
  code: string
  collectionDate: string
  totalLiters: number
  goatCount: number
}

export interface BatchOutput {
  outputId: number
  productId: number
  sku: string
  productName: string
  saleUnit: SaleUnit
  quantity: number
  consumed: number
  available: number
}

export interface CheeseBatch {
  id: number
  code: string
  milkLotId: number
  milkLotCode: string
  collectionDate: string
  milkLotLiters: number
  madeOn: string
  expiresOn: string
  litersUsed: number
  notes: string | null
  outputs: BatchOutput[]
}

export interface CheesePurchase {
  id: number
  code: string
  supplierId: number
  supplierName: string
  productId: number
  sku: string
  productName: string
  purchaseDate: string
  expiresOn: string
  quantity: number
  consumed: number
  available: number
  unitCost: number
  total: number
  notes: string | null
}

export interface StockItem {
  source: StockSource
  sourceId: number
  parentId: number
  code: string
  productId: number
  sku: string
  productName: string
  saleUnit: SaleUnit
  entryDate: string
  expiresOn: string
  daysToExpire: number
  quantityIn: number
  adjusted: number
  sold: number
  available: number
}

export interface CheeseSummary {
  products: { productId: number; sku: string; productName: string; saleUnit: SaleUnit; available: number }[]
  expiringSoon: number
  expired: number
  lastBatch: CheeseBatch | null
}

export interface StockAdjustment {
  id: number
  adjustmentDate: string
  reason: AdjustmentReason
  quantity: number
  notes: string | null
  source: StockSource
  sourceId: number
  code: string
  productName: string
  saleUnit: SaleUnit
}

export interface BatchInput {
  madeOn: string
  expiresOn: string
  litersUsed: number
  outputs: { productId: number; quantity: number }[]
  notes: string | null
}

export interface PurchaseInput {
  supplierId: number
  productId: number
  purchaseDate: string
  expiresOn: string
  quantity: number
  unitCost: number
  notes: string | null
}

export interface AdjustmentInput {
  source: StockSource
  sourceId: number
  adjustmentDate: string
  reason: AdjustmentReason
  quantity: number
  notes: string | null
}
