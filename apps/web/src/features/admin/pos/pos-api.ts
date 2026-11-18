import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { toBuildPick, toItemPick, type BuildPick, type ItemPick } from "./pos-rules";

export type PosSaleRequest = Schemas["PosSaleRequest"];
export type PosPreview = Schemas["PosPreview"];
export type PosCustomer = Schemas["PosCustomerView"];

const api = () => browserApi();
const SEARCH_SIZE = 12;

/**
 * Items in stock matching the search. Staff who can see the inventory search every stock item (hidden ones too);
 * others search what the store lists.
 */
export async function searchItems(search: string, fullInventory: boolean): Promise<ItemPick[]> {
  if (fullInventory) {
    const page = await required(unwrap(api().GET("/api/v1/admin/retail/stock-items", {
      params: { query: { search: search || undefined, inStock: true, pageSize: SEARCH_SIZE } }
    })));
    return page.items.map(toItemPick);
  }
  const page = await required(unwrap(api().GET("/api/v1/store/items", {
    params: { query: { search: search || undefined, inStock: true, pageSize: SEARCH_SIZE } }
  })));
  return page.items.map(toItemPick);
}

/**
 * Builds that can be sold here. Staff who manage builds see drafts too, with each part (so a build can be sold part by part);
 * others see the published builds, sold whole.
 */
export async function listBuilds(manageBuilds: boolean): Promise<BuildPick[]> {
  if (manageBuilds) {
    const builds = await required(unwrap(api().GET("/api/v1/admin/retail/assemblies")));
    return builds.filter((build) => build.status === "Published" || build.status === "Draft").map(toBuildPick);
  }
  const builds = await required(unwrap(api().GET("/api/v1/store/assemblies", { params: { query: {} } })));
  return builds.map(toBuildPick);
}

export const previewSale = (body: PosSaleRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/pos/preview", { body })));

export const createSale = (body: PosSaleRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/pos/sales", { body })));

/** "عميل قديم": the latest details used with this phone, or null when it has no order yet. */
export async function findCustomer(phone: string): Promise<PosCustomer | null> {
  try {
    return (await unwrap(api().GET("/api/v1/admin/retail/pos/customers", { params: { query: { phone } } }))) ?? null;
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}
