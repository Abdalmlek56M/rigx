import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type RequestPage = Schemas["CustomerRequestPage"];
export type RequestRow = Schemas["CustomerRequestListItem"];
export type RequestDetail = Schemas["CustomerRequestView"];
export type WhatsAppReply = Schemas["WhatsAppReplyView"];
export type BackInStockItem = Schemas["BackInStockListItem"];

const api = () => browserApi();

export const fetchRequests = (query: { type?: string; status?: string; q?: string; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/requests", { params: { query: { ...query, pageSize: 25 } } })));

export const fetchRequest = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/requests/{id}", { params: { path: { id } } })));

export const fetchWhatsAppReply = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/requests/{id}/whatsapp", { params: { path: { id } } })));

export const fetchBackInStock = () =>
  required(unwrap(api().GET("/api/v1/admin/requests/back-in-stock")));

export const setRequestStatus = (id: string, status: string, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/requests/{id}/status", { params: { path: { id } }, body: { status, rowVersion } })));

export const saveOffer = (id: string, offerAmount: number, expectedResale: number | null, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/requests/{id}/offer", { params: { path: { id } }, body: { offerAmount, expectedResale, rowVersion } })));

export const decideRequest = (id: string, decision: "Accepted" | "Rejected", rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/requests/{id}/decision", { params: { path: { id } }, body: { decision, rowVersion } })));

export const saveRequestNote = (id: string, internalNote: string, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/requests/{id}/note", { params: { path: { id } }, body: { internalNote, rowVersion } })));

export const convertRequest = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/requests/{id}/convert", { params: { path: { id } }, body: { rowVersion } })));

export const markBackInStockSent = (id: string) =>
  required(unwrap(api().POST("/api/v1/admin/requests/{id}/back-in-stock/sent", { params: { path: { id } } })));

/** The private admin photo URL (same-origin, so the session cookie applies); `index` is 0-based. */
export const requestPhotoUrl = (id: string, index: number) => `/api/v1/admin/requests/${id}/photos/${index}`;
