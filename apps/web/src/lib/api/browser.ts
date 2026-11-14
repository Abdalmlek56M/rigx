import { createRigxClient, type RigxClient } from "@rigx/api-client";

let client: RigxClient | undefined;

/** The one browser-side API client (same origin, so the `/api` rewrite and the session cookie apply). */
export function browserApi(): RigxClient {
  client ??= createRigxClient();
  return client;
}
