import "server-only";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

const BAD_REQUEST = 400;
const TOO_MANY_REQUESTS = 429;

export type WarrantyLookup =
  | { status: "ok"; items: Schemas["WarrantyPublicView"][] }
  | { status: "rateLimited" | "badQuery" | "failed" };

/** Looks warranties up by item code or serial (never cached); failures become a status the page words. */
export async function lookupWarranty(query: string): Promise<WarrantyLookup> {
  try {
    const result = await required(unwrap(createServerApi({ fresh: true }).GET("/api/v1/warranty/lookup", { params: { query: { q: query } } })));
    return { status: "ok", items: result.items };
  } catch (error) {
    if (error instanceof ApiError && error.status === TOO_MANY_REQUESTS) return { status: "rateLimited" };
    if (error instanceof ApiError && error.status === BAD_REQUEST) return { status: "badQuery" };
    return { status: "failed" };
  }
}
