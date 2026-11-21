import { createRigxClient, type RigxClient } from "@rigx/api-client";

let client: RigxClient | undefined;

/** The page's language (`<html lang>`), sent as `Accept-Language` so the API answers errors in the same language. */
export function pageLanguage(): string {
  return typeof document === "undefined" ? "" : document.documentElement.lang;
}

/** Adds the page language to every request; a Request keeps its method, body and credentials when copied. */
export const fetchInPageLanguage: typeof fetch = (input, init) => {
  const request = new Request(input, init);
  const language = pageLanguage();
  if (language) request.headers.set("Accept-Language", language);
  return fetch(request);
};

/** The one browser-side API client (same origin, so the `/api` rewrite and the session cookie apply). */
export function browserApi(): RigxClient {
  client ??= createRigxClient({ fetch: fetchInPageLanguage });
  return client;
}
