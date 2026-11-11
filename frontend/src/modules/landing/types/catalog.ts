export type Division = 'cheese' | 'goat' | 'cattle'

export interface Presentation {
  id: string
  /** SKU del catálogo `products` en database.sql. */
  sku: string
  label: string
  /** Precio en MXN; null cuando el producto se cotiza. */
  price: number | null
}

export interface Product {
  id: string
  division: Division
  badge: string
  subtitle: string
  name: string
  description: string
  specLabel: string
  spec: string
  priceLabel: string
  image: string
  imageAlt: string
  presentations: Presentation[]
}

export interface OrderLine {
  key: string
  productName: string
  presentationLabel: string
  price: number | null
  quantity: number
}

export interface TraceRecord {
  code: string
  division: Division
  sampleLabel: string
  badge: string
  title: string
  fields: { label: string; value: string }[]
}
