import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type CustomerPage = Schemas["PagedResultOfCustomerListItem"];
export type CustomerRow = Schemas["CustomerListItem"];
export type CustomerProfile = Schemas["CustomerProfile"];
export type CustomerViewAs = Schemas["CustomerViewAs"];
export type BlacklistEntry = Schemas["BlacklistView"];
export type ShippingRates = Schemas["ShippingRatesView"];

const api = () => browserApi();

export const fetchCustomers = (query: { search?: string; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/customers", { params: { query } })));

export const fetchCustomer = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/customers/{id}", { params: { path: { id } } })));

export const fetchCustomerViewAs = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/customers/{id}/view-as", { params: { path: { id } } })));

export const saveCustomerNote = (id: string, note: string) =>
  unwrap(api().PUT("/api/v1/admin/customers/{id}/note", { params: { path: { id } }, body: { note } }));

export const sendCustomerLoginCode = (id: string) =>
  required(unwrap(api().POST("/api/v1/admin/customers/{id}/login-code", { params: { path: { id } } })));

export const unlinkCustomerGoogle = (id: string) =>
  unwrap(api().POST("/api/v1/admin/customers/{id}/google/unlink", { params: { path: { id } } }));

export const signOutCustomer = (id: string) =>
  unwrap(api().POST("/api/v1/admin/customers/{id}/sign-out-all", { params: { path: { id } } }));

export const fetchShippingRates = () =>
  required(unwrap(api().GET("/api/v1/storefront/shipping-rates")));

export const fetchBlacklist = () => required(unwrap(api().GET("/api/v1/admin/retail/blacklist")));

export const addToBlacklist = (customer: { name: string; phone: string | null; email: string }, reason: string) =>
  required(unwrap(api().POST("/api/v1/admin/retail/blacklist", {
    body: { orderId: null, name: customer.name, phone: customer.phone, email: customer.email, address: null, reason }
  })));

export const removeFromBlacklist = (id: string) =>
  unwrap(api().DELETE("/api/v1/admin/retail/blacklist/{id}", { params: { path: { id } } }));
