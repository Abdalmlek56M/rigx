import { request, type APIRequestContext } from "@playwright/test";
import { BASE_URL, previewKey } from "./env";

export interface PreviewCode {
  code: string;
  purpose: string;
  expiresAtUtc: string;
}

const previewHeaders = () => ({ "X-Preview-Bootstrap-Key": previewKey() });

/** A request context with its own cookie jar, aimed at the API through the web origin (the cookies and CSRF match the browser's). */
export const newApi = (): Promise<APIRequestContext> => request.newContext({ baseURL: BASE_URL, timeout: 120_000 });

/** POSTs JSON the way the browser does: every unsafe call first fetches the anti-forgery token for this cookie jar. */
export async function postJson<T>(api: APIRequestContext, path: string, body?: unknown, headers: Record<string, string> = {}): Promise<T> {
  const { requestToken } = (await (await api.get("/api/v1/identity/antiforgery")).json()) as { requestToken: string };
  const response = await api.post(path, { data: body, headers: { "X-CSRF-TOKEN": requestToken, ...headers } });
  if (!response.ok()) throw new Error(`POST ${path} answered ${response.status()}: ${await response.text()}`);
  return (await response.json()) as T;
}

export async function getJson<T>(api: APIRequestContext, path: string): Promise<T> {
  const response = await api.get(path);
  if (!response.ok()) throw new Error(`GET ${path} answered ${response.status()}: ${await response.text()}`);
  return (await response.json()) as T;
}

/** Seeds the demo store (idempotent). */
export async function initializePreview(api: APIRequestContext): Promise<void> {
  await postJson(api, "/api/v1/development/preview/initialize", undefined, previewHeaders());
}

/** The newest sign-in code the dev API kept for an address, or null when none was sent yet. */
export async function latestCode(api: APIRequestContext, email: string): Promise<PreviewCode | null> {
  const response = await api.get(`/api/v1/development/preview/email-code?email=${encodeURIComponent(email)}`, { headers: previewHeaders() });
  return response.ok() ? ((await response.json()) as PreviewCode) : null;
}
