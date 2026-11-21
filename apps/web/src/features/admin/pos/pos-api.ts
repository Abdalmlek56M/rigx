import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { offeredOptions, PRICE_OPTIONS, toBuildPick, toItemPick, type BuildPick, type ItemPick, type PriceOption } from "./pos-rules";

export type PosSaleRequest = Schemas["PosSaleRequest"];
export type PosPreview = Schemas["PosPreview"];
export type PosCustomer = Schemas["PosCustomerView"];

const api = () => browserApi();
const SEARCH_SIZE = 12;

type CategoryNode = { id: string; priceOptions?: { alone: boolean; withBoard: boolean; inBuild: boolean } | null; children?: CategoryNode[] };

/** The sale prices each category offers (from the public category tree), as `PriceOption`s; a category not listed offers all three. */
async function loadOptionsOf(): Promise<(categoryId: string | null) => readonly PriceOption[]> {
  const byId = new Map<string, readonly PriceOption[]>();
  const walk = (nodes: readonly CategoryNode[]) => {
    for (const node of nodes) {
      const set = node.priceOptions;
      if (set) byId.set(node.id, offeredOptions([set.alone ? "Alone" : "", set.withBoard ? "WithBoard" : "", set.inBuild ? "InBuild" : ""]));
      walk(node.children ?? []);
    }
  };
  try {
    walk(await required(unwrap(api().GET("/api/v1/catalog/categories"))));
  } catch {
    // Without the tree every price stays offered; the server still decides what an order may use.
  }
  return (categoryId) => (categoryId ? byId.get(categoryId) : undefined) ?? PRICE_OPTIONS;
}

/**
 * Items in stock matching the search. Staff who can see the inventory search every stock item (hidden ones too);
 * others search what the store lists.
 */
export async function searchItems(search: string, fullInventory: boolean): Promise<ItemPick[]> {
  if (fullInventory) {
    const [page, optionsOf] = await Promise.all([
      required(unwrap(api().GET("/api/v1/admin/retail/stock-items", {
        params: { query: { search: search || undefined, inStock: true, pageSize: SEARCH_SIZE } }
      }))),
      loadOptionsOf()
    ]);
    return page.items.map((row) => toItemPick(row, optionsOf));
  }
  const page = await required(unwrap(api().GET("/api/v1/store/items", {
    params: { query: { search: search || undefined, inStock: true, pageSize: SEARCH_SIZE } }
  })));
  return page.items.map((row) => toItemPick(row));
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

/** "عميل قديم": past buyers matching part of a phone or a name, newest first (see `customerSearchReady`). */
export const searchCustomers = (search: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/pos/customers", { params: { query: { search } } })));
