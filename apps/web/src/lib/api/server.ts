import { createRigxClient, type RigxClient } from "@rigx/api-client";

/** Seconds server-fetched storefront data (settings, menu) stays cached. */
const REVALIDATE_SECONDS = 300;

const cachedFetch: typeof fetch = (input, init) => fetch(input, { ...init, next: { revalidate: REVALIDATE_SECONDS } });

const freshFetch: typeof fetch = (input, init) => fetch(input, { ...init, cache: "no-store" });

/**
 * API client for server components: absolute origin, no cookies forwarded unless `cookie` is given (the visitor's own cookie header,
 * for a page that shows what only the signed-in visitor may see, such as a private build). Responses are cached unless `fresh`
 * (per-customer data); a call with a `cookie` must be `fresh`.
 */
export function createServerApi({ fresh = false, cookie }: { fresh?: boolean; cookie?: string } = {}): RigxClient {
  return createRigxClient({
    baseUrl: process.env.RIGX_API_ORIGIN ?? "http://localhost:8080",
    fetch: fresh || cookie ? freshFetch : cachedFetch,
    ...(cookie ? { headers: { cookie } } : {})
  });
}
