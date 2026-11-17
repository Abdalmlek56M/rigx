import "server-only";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const BAD_REQUEST = 400;
const TOO_MANY_REQUESTS = 429;

export type WarrantyLookup =
  | { status: "ok"; groups: Schemas["WarrantyLookupGroup"][] }
  | { status: "rateLimited" | "badQuery" | "failed" };

/** Looks warranties up by phone, order, invoice, code or serial (never cached); failures become a status the page words. */
export async function lookupWarranty(query: string): Promise<WarrantyLookup> {
  try {
    const result = await required(unwrap(createServerApi({ fresh: true }).GET("/api/v1/warranty/lookup", { params: { query: { q: query } } })));
    return { status: "ok", groups: result.groups };
  } catch (error) {
    if (error instanceof ApiError && error.status === TOO_MANY_REQUESTS) return { status: "rateLimited" };
    if (error instanceof ApiError && error.status === BAD_REQUEST) return { status: "badQuery" };
    return { status: "failed" };
  }
}
