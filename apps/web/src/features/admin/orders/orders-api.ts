import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Order = Schemas["OrderView"];
export type OrderDetail = Schemas["AdminOrderDetailView"];
export type OrderUnit = Schemas["AdminOrderUnitView"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });

/** The newest 300 orders (all channels), optionally of one status; search and channel are filtered on the page. */
export const fetchOrders = (status: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/orders", { params: { query: { status: status || undefined } } })));

export const fetchOrder = (id: string) => required(unwrap(api().GET("/api/v1/admin/retail/orders/{id}", path(id))));

export const setOrderStatus = (id: string, status: string, notes: string | null, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/orders/{id}/status", { ...path(id), body: { status, notes, rowVersion } })));

export const completeOrder = (id: string, notes: string | null, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/orders/{id}/complete", { ...path(id), body: { notes, rowVersion } })));

export const addOrderPayment = (id: string, method: string, amount: number, note: string | null, rowVersion: number, moneyAccountId: string | null) =>
  required(unwrap(api().POST("/api/v1/admin/retail/orders/{id}/payments", { ...path(id), body: { method, amount, note, rowVersion, moneyAccountId } })));

export const setPaymentStatus = (id: string, paymentStatus: string, notes: string | null, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/orders/{id}/payment", { ...path(id), body: { paymentStatus, notes, rowVersion } })));

export const waivePayment = (id: string, reason: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/orders/{id}/payment/waive", { ...path(id), body: { reason, rowVersion } })));

export const waiveDeposit = (id: string, reason: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/orders/{id}/deposit/waive", { ...path(id), body: { reason, rowVersion } })));

export const setShipping = (id: string, courierName: string | null, waybillNumber: string | null, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/orders/{id}/shipping", { ...path(id), body: { courierName, waybillNumber, rowVersion } })));

/** `hours` restarts the hold from now; null keeps the current end and only changes what happens when it ends. */
export const setReservation = (id: string, hours: number | null, autoRelease: boolean, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/orders/{id}/reservation", { ...path(id), body: { hours, autoRelease, rowVersion } })));

/** The order's invoice (issued when it is delivered); its `accessToken` opens the public page `/i/{token}`. */
export const fetchOrderInvoice = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/orders/{id}/invoice", path(id))));

/** The blacklist list and removal are shared with the customer page. */
export { fetchBlacklist, removeFromBlacklist, type BlacklistEntry } from "../customers/customers-api";

/** Blacklists the order's customer (phone, email and address are copied from the order). */
export const blacklistOrderCustomer = (orderId: string, reason: string) =>
  required(unwrap(api().POST("/api/v1/admin/retail/blacklist", {
    body: { orderId, name: null, phone: null, email: null, address: null, reason }
  })));
