/** Rules for the inventory screen: list figures, status, filters and the edit form. Pure, so it is tested. Messages live under `admin.inventory`. */
import type { Category } from "../categories/categories-api";
import { findPath } from "../categories/category-rules.ts";
import type { Partner } from "../expenses/expenses-api";
import { amountOk, amountText, optionalAmountOk, parseAmount, partnerShare } from "../stock/stock-fields.ts";
import type { StockItem, StockItemUpdateRequest, StockUnit } from "./inventory-api";

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
    internalNote: item.internalNote ?? ""
  };
}

/** The form can be sent: a model name, a price and extras that are not negative (the server still decides). */
export function itemFormValid(form: StockItemForm): boolean {
  return form.modelName.trim() !== "" && amountOk(form.price) && optionalAmountOk(form.withBoardExtra) && optionalAmountOk(form.aloneExtra);
}

/** The update request: the form's fields, the item's own `productId` and `specs` kept as read, and its `rowVersion`. */
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
    specs: item.specs,
    rowVersion: item.rowVersion
  };
}

/** The percent typed in the unit's partner dialog: empty = the partner's default (`null`); no partner = no percent. */
export function unitPercent(partnerId: string, percentText: string): number | null {
  return partnerId ? parseAmount(percentText) : null;
}
