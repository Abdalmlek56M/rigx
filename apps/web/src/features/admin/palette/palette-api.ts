import { fetchCustomers } from "../customers/customers-api";
import { fetchStockItems } from "../inventory/inventory-api";
import { filterOrders } from "../orders/order-rules";
import { fetchOrders, type Order } from "../orders/orders-api";
import { fetchPurchases } from "../purchases/purchases-api";
import { customerEntry, itemEntry, orderEntry, purchaseEntry, RECORDS_PER_KIND, type PaletteEntry, type RecordSource } from "./palette-rules";

/** The orders list has no server search (the page filters its newest 300), so the palette keeps one copy for a minute and filters it the same way. */
const ORDERS_TTL_MS = 60_000;
let cachedOrders: { at: number; load: Promise<readonly Order[]> } | null = null;

function newestOrders(): Promise<readonly Order[]> {
  if (cachedOrders && Date.now() - cachedOrders.at < ORDERS_TTL_MS) return cachedOrders.load;
  const load = fetchOrders("");
  cachedOrders = { at: Date.now(), load };
  // A failed load must not be served again for a minute.
  load.catch(() => {
    if (cachedOrders?.load === load) cachedOrders = null;
  });
  return load;
}

const SEARCHES: Record<RecordSource, (query: string) => Promise<PaletteEntry[]>> = {
  orders: async (query) => filterOrders(await newestOrders(), { q: query, channel: "" }).slice(0, RECORDS_PER_KIND).map(orderEntry),
  customers: async (query) => (await fetchCustomers({ search: query, page: 1 })).items.slice(0, RECORDS_PER_KIND).map((row) => customerEntry(row.customer)),
  items: async (query) => (await fetchStockItems({ search: query, page: 1 })).items.slice(0, RECORDS_PER_KIND).map(itemEntry),
  purchases: async (query) => (await fetchPurchases({ q: query, page: 1 })).items.slice(0, RECORDS_PER_KIND).map(purchaseEntry)
};

export interface RecordResults {
  entries: PaletteEntry[];
  /** At least one kind of record could not be searched. */
  failed: boolean;
}

/** Looks the text up among the kinds of record the person may open; one that fails does not hide the others. */
export async function searchRecords(query: string, sources: readonly RecordSource[]): Promise<RecordResults> {
  const settled = await Promise.allSettled(sources.map((source) => SEARCHES[source](query)));
  return {
    entries: settled.flatMap((result) => (result.status === "fulfilled" ? result.value : [])),
    failed: settled.some((result) => result.status === "rejected")
  };
}
