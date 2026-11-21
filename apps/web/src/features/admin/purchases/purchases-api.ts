import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Purchase = Schemas["PurchaseView"];
export type PurchaseListItem = Schemas["PurchaseListItem"];
export type PurchaseLineView = Schemas["PurchaseLineView"];
export type PurchaseRequest = Schemas["PurchaseRequest"];
export type PurchaseLineRequest = Schemas["PurchaseLineRequest"];
export type PurchaseReceived = Schemas["PurchaseReceived"];
export type Seller = Schemas["SellerView"];
export type SellerSummary = Schemas["SellerSummary"];
export type SellerRequest = Schemas["SellerRequest"];
export type ModelSuggestion = Schemas["ModelNameSuggestion"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });

export interface PurchaseQuery {
  status?: string;
  q?: string;
  page: number;
}

export const fetchPurchases = (query: PurchaseQuery) =>
  required(unwrap(api().GET("/api/v1/admin/purchasing/purchases", { params: { query } })));

export const fetchPurchase = (id: string) => required(unwrap(api().GET("/api/v1/admin/purchasing/purchases/{id}", path(id))));

export const createPurchase = (body: PurchaseRequest) =>
  required(unwrap(api().POST("/api/v1/admin/purchasing/purchases", { body })));

export const updatePurchase = (id: string, body: PurchaseRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/purchasing/purchases/{id}", { ...path(id), body })));

/** Creates the RX stock items; the server receives the saved draft as it is (no body). */
export const receivePurchase = (id: string) => required(unwrap(api().POST("/api/v1/admin/purchasing/purchases/{id}/receive", path(id))));

/** Pays the seller of a received purchase part of what is still owed; the money leaves the default account of the method. */
export const paySeller = (id: string, amount: number, method: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/purchasing/purchases/{id}/payments", { ...path(id), body: { amount, method, rowVersion } })));

export const cancelPurchase = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/purchasing/purchases/{id}/cancel", { ...path(id), body: { rowVersion } })));

/** Sellers by phone (any spelling) or by name/phone text. */
export const findSellers = (query: { phone?: string; q?: string }) =>
  required(unwrap(api().GET("/api/v1/admin/purchasing/sellers", { params: { query } })));

export const fetchSeller = (id: string) => required(unwrap(api().GET("/api/v1/admin/purchasing/sellers/{id}", path(id))));

export const createSeller = (body: SellerRequest) => required(unwrap(api().POST("/api/v1/admin/purchasing/sellers", { body })));

export const updateSeller = (id: string, body: SellerRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/purchasing/sellers/{id}", { ...path(id), body })));

/** Model names from stock and the catalog that look like what is typed (a hint, never forced). */
export const suggestModels = (q: string, categoryId: string | undefined) =>
  required(unwrap(api().GET("/api/v1/admin/retail/models/suggest", { params: { query: { q, categoryId } } })));
