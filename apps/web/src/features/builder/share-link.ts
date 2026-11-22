import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { builderPath, cleanShareName, sharedBuildUrl, type PartInput } from "./builder-state";

export interface ShareLink {
  url: string;
  /** `true` when the build was saved on the server (the link then survives price and stock changes); else the link lists the parts. */
  saved: boolean;
}

/**
 * The link to this build: saved on the server when that works (anyone may save; the page it opens names the build and, for a signed-in
 * visitor, who assembled it), otherwise the plain `?parts=` link (a rate-limited or refused save never blocks sharing).
 */
export async function createShareLink(parts: readonly PartInput[], name: string): Promise<ShareLink> {
  const origin = window.location.origin;
  const path = window.location.pathname;
  try {
    const saved = await required(unwrap(browserApi().POST("/api/v1/builder/saved", { body: { name: cleanShareName(name), usage: null, parts: [...parts] } })));
    return { url: sharedBuildUrl(origin, path, saved.token), saved: true };
  } catch {
    return { url: `${origin}${path.replace(/\/builder$/, "")}${builderPath(parts)}`, saved: false };
  }
}
