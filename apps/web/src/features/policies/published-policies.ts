import "server-only";
import { loadPolicy } from "./load-policy";
import { POLICY_KINDS, type PolicySlug } from "./policy-body";

/** Slugs of the published policies, in display order. A policy the API cannot load right now counts as unpublished, so no link ever leads to a 404. */
export async function publishedPolicySlugs(): Promise<PolicySlug[]> {
  const found = await Promise.all(
    POLICY_KINDS.map(async (kind) => {
      try {
        return (await loadPolicy(kind.api)) ? kind.slug : null;
      } catch {
        return null;
      }
    })
  );
  return found.filter((slug): slug is PolicySlug => slug !== null);
}
