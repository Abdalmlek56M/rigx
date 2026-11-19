import "server-only";
import { unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import type { Condition } from "@/lib/condition";

/** The newest available store items, optionally of one condition or category; an API failure just means an empty list. */
export async function newestItems(count: number, filter: { condition?: Condition; categoryId?: string } = {}): Promise<Schemas["StoreItemCard"][]> {
  try {
    const page = await required(
      unwrap(createServerApi().GET("/api/v1/store/items", { params: { query: { inStock: true, sort: "newest", pageSize: count, ...filter } } }))
    );
    return page.items;
  } catch {
    return [];
  }
}
