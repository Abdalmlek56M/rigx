import "server-only";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const NOT_FOUND = 404;

/** `feed: null` = the owner turned the sold feed off (the page is then a 404); `failed` = the API did not answer. */
export type SoldResult = { status: "ok"; page: Schemas["PagedResultOfSoldFeedItem"] } | { status: "off" } | { status: "failed" };

export async function loadSold(query: { kind?: string; categoryId?: string; page: number }): Promise<SoldResult> {
  try {
    return { status: "ok", page: await required(unwrap(createServerApi().GET("/api/v1/store/sold", { params: { query } }))) };
  } catch (error) {
    if (error instanceof ApiError && error.status === NOT_FOUND) return { status: "off" };
    console.warn("Sold feed unavailable:", error instanceof Error ? error.message : error);
    return { status: "failed" };
  }
}

/** Governorate code → name for the shipped-to line; empty when the API does not answer (the stored text is shown as it is). */
export async function loadGovernorateNames(): Promise<Map<string, string>> {
  try {
    const rates = await required(unwrap(createServerApi().GET("/api/v1/storefront/shipping-rates")));
    return new Map(rates.governorates.map((entry) => [entry.code.toUpperCase(), entry.name]));
  } catch {
    return new Map();
  }
}
