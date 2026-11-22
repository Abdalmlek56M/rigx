/** Table density: how tight the admin list rows are. The choice is kept per signed-in user in this browser. Pure, so it is tested. */
export const DENSITIES = ["comfortable", "compact"] as const;
export type Density = (typeof DENSITIES)[number];

export const DEFAULT_DENSITY: Density = "comfortable";

/** One key per user, so two staff members sharing a browser keep their own choice. */
export const densityStorageKey = (username: string): string => `rigx.admin.density.${username}`;

/** The other density (the palette's "switch density" action). */
export const nextDensity = (density: Density): Density => (density === "compact" ? "comfortable" : "compact");

/** A stored value that is not a known density reads as the default. */
export function parseDensity(value: string | null): Density {
  return DENSITIES.find((density) => density === value) ?? DEFAULT_DENSITY;
}
