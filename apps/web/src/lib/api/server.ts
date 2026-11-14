import { createRigxClient, type RigxClient } from "@rigx/api-client";

/** Seconds server-fetched storefront data (settings, menu) stays cached. */
const REVALIDATE_SECONDS = 300;

const cachedFetch: typeof fetch = (input, init) => fetch(input, { ...init, next: { revalidate: REVALIDATE_SECONDS } });

/** API client for server components: absolute origin, cached responses, no cookies forwarded. */
export function createServerApi(): RigxClient {
  return createRigxClient({
    baseUrl: process.env.RIGX_API_ORIGIN ?? "http://localhost:8080",
    fetch: cachedFetch
  });
}
