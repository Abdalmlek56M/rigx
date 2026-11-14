import { browserApi } from "@/lib/api/browser";
import { reconcile, type CartLine, type FreshItem } from "./cart-rules";

const NOT_FOUND = 404;

async function fetchFresh(code: string): Promise<FreshItem | null | undefined> {
  const { data, response } = await browserApi().GET("/api/v1/store/items/{code}", { params: { path: { code } } });
  if (response.status === NOT_FOUND) return null;
  return data ? { available: data.available, prices: data.prices } : undefined;
}

/** Re-reads every line's item from the API; a failed request leaves that line untouched. */
export async function refreshLines(lines: CartLine[]): Promise<{ lines: CartLine[]; removed: number }> {
  const codes = [...new Set(lines.map((line) => line.code))];
  const results = await Promise.all(codes.map(async (code) => [code, await fetchFresh(code).catch(() => undefined)] as const));
  const fresh = new Map<string, FreshItem | null>();
  for (const [code, item] of results) if (item !== undefined) fresh.set(code, item);
  return reconcile(lines, fresh);
}
