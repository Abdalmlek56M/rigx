import type { Schemas } from "@rigx/api-client";

type RelatedBuild = Schemas["AssemblyPublicView"];

/** The build rail shows at most this many other builds. */
export const RELATED_BUILDS_MAX = 12;

/** Other published builds, the ones with the same usage first, capped for the rail. */
export function pickRelatedBuilds(currentCode: string, currentUsage: string | null, builds: readonly RelatedBuild[]): RelatedBuild[] {
  const others = builds.filter((build) => build.code !== currentCode);
  const sameUsage = currentUsage ? others.filter((build) => build.usage === currentUsage) : [];
  const rest = others.filter((build) => !sameUsage.includes(build));
  return [...sameUsage, ...rest].slice(0, RELATED_BUILDS_MAX);
}
