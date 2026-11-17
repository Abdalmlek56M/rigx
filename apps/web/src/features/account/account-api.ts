import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Order = Schemas["CustomerOrderView"];

const api = () => browserApi();

export const fetchOrders = () => required(unwrap(api().GET("/api/v1/orders/mine")));

/** One of the person's orders, or `null` when it does not exist (or is not theirs). */
export async function fetchOrder(id: string): Promise<Order | null> {
  try {
    return await required(unwrap(api().GET("/api/v1/orders/mine/{id}", { params: { path: { id } } })));
  } catch (error) {
    if (error instanceof ApiError && (error.status === 404 || error.status === 400)) return null;
    throw error;
  }
}

export const fetchWarranties = () => required(unwrap(api().GET("/api/v1/warranties/mine")));
export const fetchInvoices = () => required(unwrap(api().GET("/api/v1/invoices/mine")));
export const fetchRequests = () => required(unwrap(api().GET("/api/v1/account/requests")));
export const fetchBuilds = () => required(unwrap(api().GET("/api/v1/account/builds")));
export const fetchAddresses = () => required(unwrap(api().GET("/api/v1/identity/addresses")));
export const fetchLoginMethods = () => required(unwrap(api().GET("/api/v1/identity/login-methods")));
export const fetchSessions = () => required(unwrap(api().GET("/api/v1/identity/sessions")));
export const fetchNewDeviceAlerts = () => required(unwrap(api().GET("/api/v1/identity/settings/new-device-alerts")));
