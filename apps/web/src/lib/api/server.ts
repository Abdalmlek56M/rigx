import { createRigxClient, type RigxClient } from "@rigx/api-client";

/** Seconds server-fetched storefront data (settings, menu) stays cached. */
const REVALIDATE_SECONDS = 300;

const cachedFetch: typeof fetch = (input, init) => fetch(input, { ...init, next: { revalidate: REVALIDATE_SECONDS } });

const freshFetch: typeof fetch = (input, init) => fetch(input, { ...init, cache: "no-store" });

/** API client for server components: absolute origin, no cookies forwarded. Responses are cached unless `fresh` (per-customer data). */
export function createServerApi({ fresh = false }: { fresh?: boolean } = {}): RigxClient {
  return createRigxClient({
    baseUrl: process.env.RIGX_API_ORIGIN ?? "http://localhost:8080",
    fetch: fresh ? freshFetch : cachedFetch
  });
}
