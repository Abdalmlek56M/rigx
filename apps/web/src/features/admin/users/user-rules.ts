/** Users and roles: URL filters, username from email, role names, permission grouping, restriction requests. Pure, so it is tested. */
import type { FilterValues } from "../kit/url-filters";
import { cairoDayEndUtc } from "../offers/offer-rules.ts";
import type { RestrictionInput, StaffRole, StaffUser } from "./users-api";

export const USER_VIEWS = ["users", "roles"] as const;
export type UserView = (typeof USER_VIEWS)[number];

export function userView(value: string | undefined): UserView {
  return USER_VIEWS.find((view) => view === value) ?? "users";
}

/** Every filter the screen keeps in the URL (`user` = the picked staff member, `role` = the picked role or `new`). */
export const USER_FILTERS = { view: "users", search: "", active: "", page: "1", user: "", role: "" };

export type UserValues = FilterValues<typeof USER_FILTERS>;
export type UserSet = (changes: Partial<UserValues>) => void;

export const ACTIVE_FILTERS = ["", "true", "false"] as const;

/** The API's `isActive` from the filter value (anything else = all). */
export function activeFilter(value: string): boolean | undefined {
  return value === "true" ? true : value === "false" ? false : undefined;
}

/** The invite form's username: the part of the email before `@`, lower-cased. */
export function usernameFromEmail(email: string): string {
  return (email.split("@")[0] ?? "").trim().toLowerCase();
}

export function isSameAccount(email: string, other: string | null | undefined): boolean {
  return other != null && email.trim().toLowerCase() === other.trim().toLowerCase();
}

/** The built-in roles the server creates; their stored names are English, so the screen shows its own label. */
export const BUILT_IN_ROLES = ["Owner", "Customer"] as const;
export type BuiltInRole = (typeof BUILT_IN_ROLES)[number];

export function builtInRole(code: string): BuiltInRole | null {
  return BUILT_IN_ROLES.find((role) => role === code) ?? null;
}

/** A role's display name: the label of a built-in role, else the name the owner gave it. */
export function roleLabel(role: Pick<StaffRole, "code" | "name">, builtIn: (role: BuiltInRole) => string): string {
  const known = builtInRole(role.code);
  return known ? builtIn(known) : role.name;
}

/** The role names of a user in their own order; a role the list does not know shows its code. */
export function roleNames(
  user: Pick<StaffUser, "roleIds" | "roleCodes">,
  roles: readonly Pick<StaffRole, "id" | "code" | "name">[],
  builtIn: (role: BuiltInRole) => string
): string[] {
  return user.roleIds.map((id, index) => {
    const role = roles.find((entry) => entry.id === id);
    return role ? roleLabel(role, builtIn) : user.roleCodes[index] ?? id;
  });
}

/** A permission's readable label from the grouped messages (`Sales.ViewOrders` → labels.Sales.ViewOrders), or null when there is none. */
export function permissionLabel(code: string, labels: Readonly<Record<string, Readonly<Record<string, string>>>>): string | null {
  const [group = "", name = ""] = code.split(".");
  const names = Object.hasOwn(labels, group) ? labels[group] : undefined;
  return (names && Object.hasOwn(names, name) ? names[name] : undefined) ?? null;
}

/** Adds or removes one value from a checkbox selection. */
export function toggle(selected: readonly string[], value: string, on: boolean): string[] {
  const rest = selected.filter((entry) => entry !== value);
  return on ? [...rest, value] : rest;
}

/** The same values, whatever the order (a selection that has not changed). */
export function sameSet(first: readonly string[], second: readonly string[]): boolean {
  return first.length === second.length && first.every((entry) => second.includes(entry));
}

export const PERMISSION_GROUPS = [
  "Sales", "Inventory", "Finance", "Purchasing", "Catalog", "Customers", "Requests", "Audit", "System"
] as const;
export type PermissionGroupKey = (typeof PERMISSION_GROUPS)[number];

/** The part of a permission code before the first dot (`Sales.ViewOrders` → `Sales`). */
export function permissionPrefix(code: string): string {
  const dot = code.indexOf(".");
  return dot > 0 ? code.slice(0, dot) : code;
}

/** The translation key of a group title, or `null` for an unknown prefix (the screen shows the prefix as it is). */
export function groupTitleKey(prefix: string): PermissionGroupKey | null {
  return PERMISSION_GROUPS.find((known) => known === prefix) ?? null;
}

export interface PermissionGroup {
  prefix: string;
  codes: string[];
}

/** Codes grouped by prefix: known groups first in their fixed order, unknown ones after; codes sorted. */
export function groupPermissions(codes: readonly string[]): PermissionGroup[] {
  const groups = new Map<string, string[]>();
  for (const code of [...codes].sort()) {
    const prefix = permissionPrefix(code);
    groups.set(prefix, [...(groups.get(prefix) ?? []), code]);
  }
  const rank = (prefix: string) => {
    const index = PERMISSION_GROUPS.findIndex((known) => known === prefix);
    return index === -1 ? PERMISSION_GROUPS.length : index;
  };
  return [...groups.entries()]
    .sort(([first], [second]) => rank(first) - rank(second) || first.localeCompare(second))
    .map(([prefix, groupCodes]) => ({ prefix, codes: groupCodes }));
}

export function groupSelected(group: PermissionGroup, selected: readonly string[]): boolean {
  return group.codes.every((code) => selected.includes(code));
}

/** The selection with the whole group switched on or off. */
export function setGroup(group: PermissionGroup, selected: readonly string[], on: boolean): string[] {
  const rest = selected.filter((code) => !group.codes.includes(code));
  return on ? [...rest, ...group.codes] : rest;
}

/** The `System.*` codes in a selection (the screen warns: they open staff, backups and data reset). */
export function systemCodes(selected: readonly string[]): string[] {
  return selected.filter((code) => permissionPrefix(code) === "System");
}

export const RESTRICTION_TYPES = ["Suspension", "Ban"] as const;
export type RestrictionType = (typeof RESTRICTION_TYPES)[number];

/** The shortest reason the API accepts (restriction and reinstatement). */
export const MIN_REASON = 5;

export interface RestrictionForm {
  type: RestrictionType;
  reason: string;
  /** Cairo day the suspension ends (`YYYY-MM-DD`); a ban has none. */
  until: string;
}

export function restrictionType(value: string): RestrictionType {
  return RESTRICTION_TYPES.find((known) => known === value) ?? "Suspension";
}

export function restrictionValid(form: RestrictionForm): boolean {
  return form.reason.trim().length >= MIN_REASON && (form.type === "Ban" || form.until !== "");
}

/** A suspension ends at the end of the picked Cairo day; a ban never ends. */
export function restrictionRequest(form: RestrictionForm): RestrictionInput {
  return {
    type: form.type,
    reason: form.reason.trim(),
    expiresAtUtc: form.type === "Suspension" ? cairoDayEndUtc(form.until) : null
  };
}

/** A role code: letters, digits, `.`, `_` or `-` only (the API refuses anything else). */
export function roleCodeValid(code: string): boolean {
  return /^[\p{L}\p{N}._-]+$/u.test(code.trim());
}
