import { multipart, type Schemas, unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type StockItem = Schemas["StockItemAdminView"];
export type StockUnit = Schemas["StockUnitView"];
export type StockItemUpdateRequest = Schemas["StockItemUpdateRequest"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });

export interface StockItemQuery {
  search?: string;
  categoryId?: string;
  condition?: string;
  hidden?: boolean;
  inStock?: boolean;
  page: number;
}

export const fetchStockItems = (query: StockItemQuery) =>
  required(unwrap(api().GET("/api/v1/admin/retail/stock-items", { params: { query } })));

export const fetchStockItem = (id: string) => required(unwrap(api().GET("/api/v1/admin/retail/stock-items/{id}", path(id))));

export const updateStockItem = (id: string, body: StockItemUpdateRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/stock-items/{id}", { ...path(id), body })));

export const setItemsHidden = (ids: string[], hidden: boolean) =>
  required(unwrap(api().POST("/api/v1/admin/retail/stock-items/visibility", { body: { ids, hidden } })));

export const uploadStockPhoto = (id: string, file: File) => {
  const form = new FormData();
  form.set("image", file);
  return required(unwrap(api().POST("/api/v1/admin/retail/stock-items/{id}/photos", { ...path(id), ...multipart(form) })));
};

export const deleteStockPhoto = (id: string, storageKey: string) =>
  unwrap(api().DELETE("/api/v1/admin/retail/stock-items/{id}/photos", { params: { path: { id }, query: { storageKey } } }));

/** `partnerId` null removes the partner from the unit; `percent` null uses the partner's default. */
export const setUnitPartner = (unitId: string, partnerId: string | null, percent: number | null, rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/units/{id}/partner", { ...path(unitId), body: { partnerId, percent, rowVersion } })));

/** The warranty labels from the store settings (needs `System.StoreSettings`). */
export const fetchWarrantyLabels = async () =>
  (await required(unwrap(api().GET("/api/v1/admin/storefront/settings")))).warrantyOptions.options?.flatMap((option) => (option.label ? [option.label] : [])) ?? [];
