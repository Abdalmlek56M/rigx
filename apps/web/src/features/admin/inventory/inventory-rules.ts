/** Rules for the inventory screen: list figures, status, filters and the edit form. Pure, so it is tested. Messages live under `admin.inventory`. */
import type { Category } from "../categories/categories-api";
import { findPath } from "../categories/category-rules.ts";
import type { Partner } from "../expenses/expenses-api";
import { amountOk, amountText, optionalAmountOk, parseAmount, partnerShare } from "../stock/stock-fields.ts";
import type { StockItem, StockItemUpdateRequest, StockUnit } from "./inventory-api";

/** The store lists an item only with at least this many photos (the API's `StockItemRules.MinPhotosToPublish`). */
export const MIN_PHOTOS_TO_PUBLISH = 3;

/** An item whose photos are too few to be listed: showing it changes nothing for a visitor until photos are added. */
export const photosTooFew = (item: Pick<StockItem, "photos">): boolean => item.photos.length < MIN_PHOTOS_TO_PUBLISH;

/** How many of these items have too few photos (the "shown, but not listed" part of a bulk show). */
export const countTooFewPhotos = (items: readonly Pick<StockItem, "photos">[]): number => items.filter(photosTooFew).length;

const AVAILABLE = "Available";
const IN_ASSEMBLY = "InAssembly";

const UNIT_STATUSES = ["Available", "InAssembly", "Sold", "PendingInspection", "Scrapped", "Returned"] as const;

/** The message key of a unit status; a status this screen does not know reads as "other". */
export function unitStatusKey(status: string): (typeof UNIT_STATUSES)[number] | "other" {
  return UNIT_STATUSES.find((known) => known === status) ?? "other";
}

export const availableUnits = (item: StockItem): StockUnit[] => item.units.filter((unit) => unit.status === AVAILABLE);

/** What the available units cost together; `null` when the server hid the costs or there is nothing available. */
export function itemCost(item: StockItem): number | null {
  const units = availableUnits(item);
  if (units.length === 0) return null;
  let total = 0;
  for (const unit of units) {
    if (unit.acquisitionCost === null) return null;
    total += unit.acquisitionCost;
  }
  return total;
}

/** The sale price of every available unit (in-build price) minus what they cost; `null` when the cost is not shown. */
export function expectedProfit(item: StockItem): number | null {
  const cost = itemCost(item);
  return cost === null ? null : item.prices.inBuild * availableUnits(item).length - cost;
}

export type StockState = "available" | "inBuild" | "soldOut";

/** Available when any unit is; else in a build when a unit sits in one; else sold out. */
export function stockState(item: StockItem): StockState {
  if (availableUnits(item).length > 0) return "available";
  return item.units.some((unit) => unit.status === IN_ASSEMBLY) ? "inBuild" : "soldOut";
}

/** The partner of the first unit with one, with the share it gets; `null` when no unit has a partner or the partner is not known. */
export function itemPartner(item: StockItem, partners: readonly Partner[]): { name: string; percent: number } | null {
  const unit = item.units.find((entry) => entry.partnerId !== null);
  const partner = unit ? partners.find((entry) => entry.id === unit.partnerId) : undefined;
  return unit && partner ? { name: partner.name, percent: partnerShare(partner, unit.partnerPercentOverride) } : null;
}

/** The build a unit sits in, with its id (for the link). */
export interface BuildGroup {
  id: string;
  code: string;
  name: string;
  status: string;
}

/** One row of the grouped stock list: a build header, or an item (indented when it sits under its build). */
export type StockRow =
  | { kind: "build"; key: string; build: BuildGroup; count: number; open: boolean }
  | { kind: "item"; key: string; item: StockItem; underBuild: boolean };

/** The build an item belongs to in the list: its unit sits in a build and nothing of it is left available; otherwise it stays loose. */
export function buildOf(item: StockItem): BuildGroup | null {
  if (availableUnits(item).length > 0) return null;
  const unit = item.units.find((entry) => entry.status === IN_ASSEMBLY && entry.assemblyId && entry.build);
  return unit?.assemblyId && unit.build ? { id: unit.assemblyId, ...unit.build } : null;
}

/**
 * The list rows: loose items as they come, and the items of a build gathered under one header row placed where its first item
 * was. A collapsed build shows only its header. Only the items of the current page are grouped.
 */
export function groupByBuild(items: readonly StockItem[], collapsed: ReadonlySet<string>): StockRow[] {
  const members = new Map<string, StockItem[]>();
  const builds = new Map<string, BuildGroup>();
  for (const item of items) {
    const build = buildOf(item);
    if (!build) continue;
    builds.set(build.id, build);
    members.set(build.id, [...(members.get(build.id) ?? []), item]);
  }
  const rows: StockRow[] = [];
  const placed = new Set<string>();
  for (const item of items) {
    const build = buildOf(item);
    if (!build) {
      rows.push({ kind: "item", key: item.id, item, underBuild: false });
      continue;
    }
    if (placed.has(build.id)) continue;
    placed.add(build.id);
    const parts = members.get(build.id) ?? [];
    const open = !collapsed.has(build.id);
    rows.push({ kind: "build", key: `build-${build.id}`, build: builds.get(build.id) ?? build, count: parts.length, open });
    if (open) rows.push(...parts.map((part): StockRow => ({ kind: "item", key: part.id, item: part, underBuild: true })));
  }
  return rows;
}

/** "كروت › AMD › RX 6000"; empty when the category is unknown. */
export function categoryPath(tree: readonly Category[], id: string | null): string {
  return id ? findPath(tree, id).map((node) => node.name).join(" › ") : "";
}

/** A "true"/"false"/empty URL value as the API's optional boolean. */
export function triState(value: string): boolean | undefined {
  return value === "true" ? true : value === "false" ? false : undefined;
}

/** Items picked in the list that still exist in the current page (a filter change drops the rest). */
export function pickedIds(selected: ReadonlySet<string>, items: readonly StockItem[]): string[] {
  return items.filter((item) => selected.has(item.id)).map((item) => item.id);
}

export interface StockItemForm {
  modelName: string;
  categoryId: string;
  brandId: string;
  price: string;
  withBoardExtra: string;
  aloneExtra: string;
  warrantyLabel: string;
  hasBox: boolean;
  isHidden: boolean;
  internalNote: string;
  specs: Record<string, string>;
}

export function toItemForm(item: StockItem): StockItemForm {
  return {
    modelName: item.modelName,
    categoryId: item.categoryId ?? "",
    brandId: item.brandId ?? "",
    price: amountText(item.price),
    withBoardExtra: amountText(item.withBoardExtra),
    aloneExtra: amountText(item.aloneExtra),
    warrantyLabel: item.warrantyLabel ?? "",
    hasBox: item.hasBox,
    isHidden: item.isHidden,
    internalNote: item.internalNote ?? "",
    specs: { ...item.specs }
  };
}

/** The form can be sent: a model name, a price and extras that are not negative (the server still decides). */
export function itemFormValid(form: StockItemForm): boolean {
  return form.modelName.trim() !== "" && amountOk(form.price) && optionalAmountOk(form.withBoardExtra) && optionalAmountOk(form.aloneExtra);
}

/** The update request: the form's fields, the item's own `productId` kept as read, and its `rowVersion`. */
export function toUpdateRequest(form: StockItemForm, item: StockItem): StockItemUpdateRequest {
  return {
    modelName: form.modelName.trim(),
    categoryId: form.categoryId || null,
    brandId: form.brandId || null,
    productId: item.productId,
    price: parseAmount(form.price) ?? 0,
    withBoardExtra: parseAmount(form.withBoardExtra),
    aloneExtra: parseAmount(form.aloneExtra),
    warrantyLabel: form.warrantyLabel.trim() || null,
    hasBox: form.hasBox,
    isHidden: form.isHidden,
    internalNote: form.internalNote.trim() || null,
    specs: form.specs,
    rowVersion: item.rowVersion
  };
}

/** The percent typed in the unit's partner dialog: empty = the partner's default (`null`); no partner = no percent. */
export function unitPercent(partnerId: string, percentText: string): number | null {
  return partnerId ? parseAmount(percentText) : null;
}

/** How long an item may sit in stock before the list flags it, in days. The one place to change the limits. */
export const STOCK_AGE_DAYS = { slow: 15, stale: 30 } as const;

export type StockAge = "fresh" | "slow" | "stale";

/** Past the first limit an item is slow (orange), past the second stale (red); an item with nothing available is never flagged. */
export function stockAge(daysInStock: number, availableCount: number): StockAge {
  if (availableCount <= 0) return "fresh";
  if (daysInStock > STOCK_AGE_DAYS.stale) return "stale";
  if (daysInStock > STOCK_AGE_DAYS.slow) return "slow";
  return "fresh";
}
