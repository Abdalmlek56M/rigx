import { browserApi } from "@/lib/api/browser";
import { buildFreshness, reconcile, type CartLine, type FreshItem } from "./cart-rules";

const NOT_FOUND = 404;

async function fetchItem(code: string): Promise<FreshItem | null | undefined> {
  const { data, response } = await browserApi().GET("/api/v1/store/items/{code}", { params: { path: { code } } });
  if (response.status === NOT_FOUND) return null;
  return data ? { available: data.available, prices: data.prices } : undefined;
}

async function fetchBuild(code: string): Promise<FreshItem | null | undefined> {
  const { data, response } = await browserApi().GET("/api/v1/store/assemblies/{code}", { params: { path: { code } } });
  if (response.status === NOT_FOUND) return null;
  return data ? buildFreshness(data) : undefined;
}

/** Re-reads every line's item or build from the API; a failed request leaves that line untouched. */
export async function refreshLines(lines: CartLine[]): Promise<{ lines: CartLine[]; removed: number }> {
  const sources = [...new Map(lines.map((line) => [line.code, line.kind])).entries()];
  const results = await Promise.all(
    sources.map(async ([code, kind]) => [code, await (kind === "Build" ? fetchBuild(code) : fetchItem(code)).catch(() => undefined)] as const)
  );
  const fresh = new Map<string, FreshItem | null>();
  for (const [code, item] of results) if (item !== undefined) fresh.set(code, item);
  return reconcile(lines, fresh);
}
