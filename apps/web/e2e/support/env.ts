import { resolve } from "node:path";

export const BASE_URL = process.env.E2E_BASE_URL ?? "http://localhost:3000";
export const OWNER_EMAIL = process.env.E2E_OWNER_EMAIL ?? "owner@example.com";

/** Local preview key of the API (never committed); the dev endpoints refuse every call without it. */
export function previewKey(): string {
  const key = process.env.E2E_PREVIEW_KEY;
  if (!key) throw new Error("Set E2E_PREVIEW_KEY to the API's Preview__BootstrapKey.");
  return key;
}

/** The owner's signed-in browser state, written once by the global setup. */
export const OWNER_STATE = resolve(import.meta.dirname, "../.auth/owner.json");

export const SIGNED_OUT = { cookies: [], origins: [] };
