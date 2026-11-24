import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { ITEM_SEARCH_SIZE, toItemHit, type ItemHit } from "./item-search-rules";

/**
 * Stock items matching `text` by name or code. Staff who may see the inventory search every item (hidden and sold out too, which a link or a
 * FAQ may point to); anyone else searches what the store lists.
 */
export async function searchItemHits(text: string, fullInventory: boolean): Promise<ItemHit[]> {
  const query = { search: text.trim(), pageSize: ITEM_SEARCH_SIZE };
  if (fullInventory) {
    const page = await required(unwrap(browserApi().GET("/api/v1/admin/retail/stock-items", { params: { query } })));
    return page.items.map(toItemHit);
  }
  const page = await required(unwrap(browserApi().GET("/api/v1/store/items", { params: { query } })));
  return page.items.map(toItemHit);
}
