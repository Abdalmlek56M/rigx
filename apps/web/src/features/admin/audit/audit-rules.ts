/** Audit screen: view/outcome mappers, Cairo days → UTC instants and change values as text. Pure, so it is tested. */
import type { FilterValues } from "../kit/url-filters";
import { cairoDayEndUtc, cairoDayStartUtc } from "../offers/offer-rules.ts";

export const AUDIT_VIEWS = ["changes", "security"] as const;
export type AuditView = (typeof AUDIT_VIEWS)[number];

export function auditView(value: string | undefined): AuditView {
  return AUDIT_VIEWS.find((view) => view === value) ?? "changes";
}

export const AUDIT_OUTCOMES = ["Succeeded", "Failed", "Rejected", "Recorded"] as const;
export type AuditOutcomeKey = (typeof AUDIT_OUTCOMES)[number] | "Other";

export function auditOutcomeKey(outcome: string): AuditOutcomeKey {
  return AUDIT_OUTCOMES.find((known) => known === outcome) ?? "Other";
}

/** Every filter the screen keeps in the URL; the entries and the events views share the dates and page. */
export const AUDIT_FILTERS = {
  view: "changes",
  module: "",
  entity: "",
  action: "",
  from: "",
  to: "",
  page: "1",
  entry: "",
  eventType: "",
  outcome: "",
  search: ""
};

export type AuditValues = FilterValues<typeof AUDIT_FILTERS>;
export type AuditSet = (changes: Partial<AuditValues>) => void;

const DATE = /^\d{4}-\d{2}-\d{2}$/;

/** The events endpoint takes UTC instants: the Cairo start of `from` and the Cairo end of `to` (null for an empty or invalid date). */
export function auditRangeUtc(from: string, to: string): { fromUtc: string | null; toUtc: string | null } {
  return {
    fromUtc: DATE.test(from) ? cairoDayStartUtc(from) : null,
    toUtc: DATE.test(to) ? cairoDayEndUtc(to) : null
  };
}

export interface ChangeRow {
  field: string;
  /** Empty when the field had no value before/after. */
  before: string;
  after: string;
}

/** One row per changed field; unknown JSON shapes are shown as their JSON text. */
export function changeRows(changes: readonly { field: string; before: unknown; after: unknown }[]): ChangeRow[] {
  return changes.map((change) => ({ field: change.field, before: formatValue(change.before), after: formatValue(change.after) }));
}

export function formatValue(value: unknown): string {
  if (value === null || value === undefined) return "";
  if (typeof value === "string") return value;
  if (typeof value === "number" || typeof value === "boolean" || typeof value === "bigint") return String(value);
  if (Array.isArray(value)) {
    if (value.length === 0) return "";
    if (value.every((item) => typeof item === "string" || typeof item === "number" || typeof item === "boolean")) return value.join(", ");
    return json(value);
  }
  if (typeof value === "object") return json(value);
  return String(value);
}

function json(value: unknown): string {
  try {
    return JSON.stringify(value) ?? "";
  } catch {
    return String(value);
  }
}

/** First characters of an id, for a compact table cell (the detail shows the whole id). */
export function shortId(value: string | null): string {
  return value ? value.slice(0, 8) : "";
}

/** A stored value's label from a message group, or the value itself when the group does not know it. */
export function labelOf(names: Readonly<Record<string, string>>, value: string): string {
  return (Object.hasOwn(names, value) ? names[value] : undefined) ?? value;
}
