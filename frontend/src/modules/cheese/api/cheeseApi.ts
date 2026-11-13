import { http } from '../../../shared/api/http'
import type {
  AdjustmentInput,
  AvailableMilkLot,
  BatchInput,
  CheeseBatch,
  CheeseCatalog,
  CheesePurchase,
  CheeseSummary,
  CheeseSupplier,
  PurchaseInput,
  StockAdjustment,
  StockItem,
} from '../types/cheese'

const base = '/cheese'

export async function getCatalog(): Promise<CheeseCatalog> {
  return (await http.get<CheeseCatalog>(`${base}/catalog`)).data
}

export async function createSupplier(input: { name: string; phone: string | null }): Promise<CheeseSupplier> {
  return (await http.post<CheeseSupplier>(`${base}/suppliers`, input)).data
}

export async function listMilkLots(): Promise<AvailableMilkLot[]> {
  return (await http.get<AvailableMilkLot[]>(`${base}/milk-lots`)).data
}

export async function listBatches(): Promise<CheeseBatch[]> {
  return (await http.get<CheeseBatch[]>(`${base}/batches`)).data
}

export async function getBatch(id: number): Promise<CheeseBatch> {
  return (await http.get<CheeseBatch>(`${base}/batches/${id}`)).data
}

export async function createBatch(input: BatchInput & { milkLotId: number }): Promise<CheeseBatch> {
  return (await http.post<CheeseBatch>(`${base}/batches`, input)).data
}

export async function updateBatch(id: number, input: BatchInput): Promise<CheeseBatch> {
  return (await http.put<CheeseBatch>(`${base}/batches/${id}`, input)).data
}

export async function deleteBatch(id: number): Promise<void> {
  await http.delete(`${base}/batches/${id}`)
}

export async function listPurchases(): Promise<CheesePurchase[]> {
  return (await http.get<CheesePurchase[]>(`${base}/purchases`)).data
}

export async function getPurchase(id: number): Promise<CheesePurchase> {
  return (await http.get<CheesePurchase>(`${base}/purchases/${id}`)).data
}

export async function createPurchase(input: PurchaseInput): Promise<CheesePurchase> {
  return (await http.post<CheesePurchase>(`${base}/purchases`, input)).data
}

export async function updatePurchase(id: number, input: PurchaseInput): Promise<CheesePurchase> {
  return (await http.put<CheesePurchase>(`${base}/purchases/${id}`, input)).data
}

export async function deletePurchase(id: number): Promise<void> {
  await http.delete(`${base}/purchases/${id}`)
}

export async function listStock(all = false): Promise<StockItem[]> {
  return (await http.get<StockItem[]>(`${base}/stock`, { params: { all } })).data
}

export async function getSummary(): Promise<CheeseSummary> {
  return (await http.get<CheeseSummary>(`${base}/summary`)).data
}

export async function listAdjustments(): Promise<StockAdjustment[]> {
  return (await http.get<StockAdjustment[]>(`${base}/adjustments`)).data
}

export async function createAdjustment(input: AdjustmentInput): Promise<StockAdjustment> {
  return (await http.post<StockAdjustment>(`${base}/adjustments`, input)).data
}

export async function deleteAdjustment(id: number): Promise<void> {
  await http.delete(`${base}/adjustments/${id}`)
}
