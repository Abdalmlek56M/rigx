import type { AdminArea, AdminGroup } from "./admin-nav";

/** Which sidebar groups a staff member folded. Kept per signed-in user in this browser. Pure, so it is tested. */

/** One key per user, so two staff members sharing a browser keep their own folds. */
export const navCollapseKey = (username: string): string => `rigx.admin.nav.collapsed.${username}`;

/** The stored list of folded group keys; anything that is not a list of text reads as "nothing folded". */
export function parseCollapsed(raw: string | null): readonly string[] {
  if (!raw) return [];
  try {
    const value: unknown = JSON.parse(raw);
    return Array.isArray(value) ? value.filter((entry): entry is string => typeof entry === "string") : [];
  } catch {
    return [];
  }
}

/** The list with `group` folded when it was open, and open when it was folded. */
export function toggleCollapsed(collapsed: readonly string[], group: string): string[] {
  return collapsed.includes(group) ? collapsed.filter((entry) => entry !== group) : [...collapsed, group];
}

/** Whether the person folded this group (the arrow always works, even on the group that holds the current page). */
export const isGroupFolded = (collapsed: readonly string[], group: AdminGroup): boolean => collapsed.includes(group.key);

/**
 * The links a group shows: all of them, or, when it is folded, only the current page's own row (if the page is in this group), so a folded
 * group still says where the person is.
 */
export function visibleAreas(collapsed: readonly string[], group: AdminGroup, active: AdminArea | null): readonly AdminArea[] {
  if (!isGroupFolded(collapsed, group)) return group.areas;
  return active !== null && group.areas.includes(active) ? [active] : [];
}
