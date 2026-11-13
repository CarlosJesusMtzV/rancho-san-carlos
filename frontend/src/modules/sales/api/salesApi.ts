import { http } from '../../../shared/api/http'
import type {
  AvailableItems,
  Customer,
  CustomerInput,
  OrderDetail,
  OrderInput,
  OrderStatus,
  OrderSummary,
  PaymentMethod,
  SalesCatalog,
  SalesSummary,
} from '../types/sales'

const base = '/sales'

export async function getCatalog(): Promise<SalesCatalog> {
  return (await http.get<SalesCatalog>(`${base}/catalog`)).data
}

export async function listCustomers(search?: string): Promise<Customer[]> {
  return (await http.get<Customer[]>(`${base}/customers`, { params: { search: search || undefined } })).data
}

export async function createCustomer(input: CustomerInput): Promise<Customer> {
  return (await http.post<Customer>(`${base}/customers`, input)).data
}

export async function updateCustomer(id: number, input: CustomerInput): Promise<Customer> {
  return (await http.put<Customer>(`${base}/customers/${id}`, input)).data
}

export async function listAvailable(date: string, orderId?: number): Promise<AvailableItems> {
  return (await http.get<AvailableItems>(`${base}/available`, { params: { date, orderId } })).data
}

export async function listOrders(status?: OrderStatus): Promise<OrderSummary[]> {
  return (await http.get<OrderSummary[]>(`${base}/orders`, { params: { status } })).data
}

export async function getOrder(id: number): Promise<OrderDetail> {
  return (await http.get<OrderDetail>(`${base}/orders/${id}`)).data
}

export async function createOrder(input: OrderInput): Promise<OrderDetail> {
  return (await http.post<OrderDetail>(`${base}/orders`, input)).data
}

export async function updateOrder(id: number, input: OrderInput): Promise<OrderDetail> {
  return (await http.put<OrderDetail>(`${base}/orders/${id}`, input)).data
}

export async function deliverOrder(id: number, input: { deliveredOn: string; paymentMethod: PaymentMethod }): Promise<OrderDetail> {
  return (await http.post<OrderDetail>(`${base}/orders/${id}/deliver`, input)).data
}

export async function cancelOrder(id: number): Promise<OrderDetail> {
  return (await http.post<OrderDetail>(`${base}/orders/${id}/cancel`)).data
}

export async function saveInvoice(id: number, input: { fiscalFolio: string; issueDate: string; taxes: number }): Promise<OrderDetail> {
  return (await http.put<OrderDetail>(`${base}/orders/${id}/invoice`, input)).data
}

export async function deleteInvoice(id: number): Promise<OrderDetail> {
  return (await http.delete<OrderDetail>(`${base}/orders/${id}/invoice`)).data
}

export async function getSummary(): Promise<SalesSummary> {
  return (await http.get<SalesSummary>(`${base}/summary`)).data
}
