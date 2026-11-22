import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { builderPath, cleanShareName, sharedBuildUrl, type PartInput } from "./builder-state";
import { DEFAULT_VISIBILITY, type Visibility } from "./build-visibility";

export interface ShareLink {
  url: string;
  /** `true` when the build was saved on the server (the link then survives price and stock changes); else the link lists the parts. */
  saved: boolean;
  /** Who may open the saved build; a link that only lists the parts is always open to anyone who has it. */
  visibility: Visibility;
}

/**
 * The link to this build: saved on the server when that works (anyone may save; the page it opens names the build and, for a signed-in
 * visitor, who assembled it), otherwise the plain `?parts=` link (a rate-limited or refused save never blocks sharing). A private or
 * public build is never turned into an open link: when its save fails the error goes to the caller.
 */
export async function createShareLink(parts: readonly PartInput[], name: string, visibility: Visibility): Promise<ShareLink> {
  const origin = window.location.origin;
  const path = window.location.pathname;
  try {
    const saved = await required(unwrap(browserApi().POST("/api/v1/builder/saved", { body: { name: cleanShareName(name), usage: null, parts: [...parts], visibility } })));
    return { url: sharedBuildUrl(origin, path, saved.token), saved: true, visibility };
  } catch (error) {
    if (visibility !== DEFAULT_VISIBILITY) throw error;
    return { url: `${origin}${path.replace(/\/builder$/, "")}${builderPath(parts)}`, saved: false, visibility };
  }
}
