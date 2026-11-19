/**
 * Rules for the build screens: list filter, status and label keys, the card and part forms, and the revenue split (sum = 100).
 * Pure, so it is tested. The server stays the authority. Messages live under `admin.builds`.
 */
import { amountOk, amountText, optionalAmountOk, parseAmount } from "../stock/stock-fields.ts";
import type { ModelSuggestion } from "../purchases/purchases-api";
import type { Build, BuildAllocationInput, BuildEditRequest, BuildNewPartBody, BuildPart, BuildPartBody, PlannedPart, PlannedPartInput } from "./builds-api";

const known = <T extends string>(list: readonly T[], value: string | null | undefined): T | "other" => list.find((entry) => entry === value) ?? "other";

export const BUILD_STATUSES = ["Draft", "Published", "Sold", "Disassembled"] as const;
export const SELL_MODES = ["Full", "FullOrSplit"] as const;

export type SellMode = (typeof SELL_MODES)[number];

export const statusKey = (status: string) => known(BUILD_STATUSES, status);
export const sellModeKey = (mode: string) => known(SELL_MODES, mode);

/** A sold or disassembled build is history: its parts and card are no longer edited. */
export const isLocked = (build: Pick<Build, "status">) => build.status === "Sold" || build.status === "Disassembled";
export const canPublish = (build: Pick<Build, "status">) => build.status === "Draft";
export const canUnpublish = (build: Pick<Build, "status">) => build.status === "Published";
export const canDisassemble = (build: Pick<Build, "status">) => build.status === "Draft" || build.status === "Published";

const normal = (text: string) => text.trim().toLocaleLowerCase();

/** The list after the status tab and the search box (code, name or a public title); an empty filter keeps everything. */
export function filterBuilds(builds: readonly Build[], status: string, search: string): Build[] {
  const query = normal(search);
  return builds.filter((build) => {
    if (status && build.status !== status) return false;
    return query === "" || [build.code, build.name, build.publicTitleAr, build.publicTitleEn].some((field) => normal(field ?? "").includes(query));
  });
}

/** The build with this id from the list a remove/replace call returned (it may also return the other build that was touched). */
export const pickBuild = (builds: readonly Build[], id: string): Build | undefined => builds.find((build) => build.id === id);

export interface CardForm {
  name: string;
  publicTitleAr: string;
  publicTitleEn: string;
  salePrice: string;
  usage: string;
  sellMode: SellMode;
  warrantyLabel: string;
  copies: string;
  suitableForAr: string;
  suitableForEn: string;
}

export function toCardForm(build: Build): CardForm {
  return {
    name: build.name,
    publicTitleAr: build.publicTitleAr ?? "",
    publicTitleEn: build.publicTitleEn ?? "",
    salePrice: amountText(build.salePrice),
    usage: build.usage ?? "",
    sellMode: SELL_MODES.find((mode) => mode === build.sellMode) ?? "FullOrSplit",
    warrantyLabel: build.warrantyLabel ?? "",
    copies: String(build.copies),
    suitableForAr: build.suitableForAr ?? "",
    suitableForEn: build.suitableForEn ?? ""
  };
}

/** A whole number of copies from 1; anything else is 0. */
export function copiesCount(text: string): number {
  const copies = Number(text);
  return Number.isInteger(copies) && copies >= 1 ? copies : 0;
}

/** A name, a sale price that is empty (the parts total is used) or not negative, and at least one copy. */
export const cardValid = (form: CardForm): boolean => form.name.trim() !== "" && optionalAmountOk(form.salePrice) && copiesCount(form.copies) > 0;

/** The edit request: an empty sale price becomes the parts total, an empty warranty means "each part's own". */
export function toEditRequest(form: CardForm, build: Build): BuildEditRequest {
  return {
    name: form.name.trim(),
    publicTitleAr: form.publicTitleAr.trim() || null,
    publicTitleEn: form.publicTitleEn.trim() || null,
    salePrice: parseAmount(form.salePrice) ?? build.partsTotal,
    usage: form.usage || null,
    sellMode: form.sellMode,
    warrantyLabel: form.warrantyLabel.trim() || null,
    copies: copiesCount(form.copies),
    suitableForAr: form.suitableForAr.trim() || null,
    suitableForEn: form.suitableForEn.trim() || null,
    rowVersion: build.rowVersion
  };
}

export interface PartForm {
  partPrice: string;
  warrantyLabel: string;
  partnerId: string;
  partnerPercent: string;
}

export const toPartForm = (part: BuildPart): PartForm => ({
  partPrice: amountText(part.partPrice),
  warrantyLabel: part.warrantyOverridden ? (part.warrantyLabel ?? "") : "",
  partnerId: part.partnerId ?? "",
  partnerPercent: amountText(part.partnerPercentOverride)
});

export const partFormValid = (form: PartForm): boolean => amountOk(form.partPrice) && optionalAmountOk(form.partnerPercent);

/** An empty warranty gives the part back the build's warranty; no partner means no percent. */
export const toPartBody = (form: PartForm, rowVersion: number): BuildPartBody => ({
  partPrice: parseAmount(form.partPrice) ?? 0,
  warrantyLabel: form.warrantyLabel.trim() || null,
  partnerId: form.partnerId || null,
  partnerPercentOverride: form.partnerId ? parseAmount(form.partnerPercent) : null,
  rowVersion
});

export interface NewPartForm {
  categoryId: string;
  brandId: string;
  specs: Record<string, string>;
  modelName: string;
  productId: string;
  condition: "New" | "Used";
  serialNumber: string;
  acquisitionCost: string;
  price: string;
  withBoardExtra: string;
  aloneExtra: string;
  partnerId: string;
  partnerPercent: string;
  isHidden: boolean;
  warrantyLabel: string;
  hasBox: boolean;
  internalNote: string;
}

export const emptyNewPart = (): NewPartForm => ({
  categoryId: "", brandId: "", specs: {}, modelName: "", productId: "", condition: "Used", serialNumber: "", acquisitionCost: "", price: "", withBoardExtra: "", aloneExtra: "",
  partnerId: "", partnerPercent: "", isHidden: false, warrantyLabel: "", hasBox: false, internalNote: ""
});

export const newPartValid = (form: NewPartForm): boolean =>
  form.modelName.trim() !== "" && amountOk(form.acquisitionCost) && amountOk(form.price)
  && optionalAmountOk(form.withBoardExtra) && optionalAmountOk(form.aloneExtra) && optionalAmountOk(form.partnerPercent);

/** The request for a part that is not in stock yet; a new part is one piece, and a serial number belongs to a used one only. */
export function toNewPartBody(form: NewPartForm, rowVersion: number): BuildNewPartBody {
  return {
    modelName: form.modelName.trim(),
    condition: form.condition,
    acquisitionCost: parseAmount(form.acquisitionCost) ?? 0,
    price: parseAmount(form.price) ?? 0,
    categoryId: form.categoryId || null,
    productId: form.productId || null,
    withBoardExtra: parseAmount(form.withBoardExtra),
    aloneExtra: parseAmount(form.aloneExtra),
    serialNumber: form.condition === "Used" ? form.serialNumber.trim() || null : null,
    partnerId: form.partnerId || null,
    partnerPercentOverride: form.partnerId ? parseAmount(form.partnerPercent) : null,
    warrantyLabel: form.warrantyLabel.trim() || null,
    hasBox: form.hasBox,
    isHidden: form.isHidden,
    internalNote: form.internalNote.trim() || null,
    brandId: form.brandId || null,
    specs: form.specs,
    rowVersion
  };
}

const round2 = (value: number) => Math.round(value * 100) / 100;

/** The split as the percent typed per part (by unit id). */
export const allocationForm = (parts: readonly BuildPart[]): Record<string, string> =>
  Object.fromEntries(parts.map((part) => [part.unitId, amountText(part.allocationPercent)]));

const percentOf = (text: string) => parseAmount(text) || 0;

/** What the typed percents add up to. */
export const allocationSum = (form: Readonly<Record<string, string>>): number => round2(Object.values(form).reduce((sum, text) => sum + percentOf(text), 0));

/** What is still missing to reach 100 (negative when over). */
export const allocationDifference = (form: Readonly<Record<string, string>>): number => round2(100 - allocationSum(form));

/** The split can be sent: every percent is a number from 0 to 100 and together they make exactly 100. */
export function allocationValid(form: Readonly<Record<string, string>>): boolean {
  const values = Object.values(form).map(parseAmount);
  return values.length > 0 && values.every((value) => value !== null && Number.isFinite(value) && value >= 0 && value <= 100) && allocationDifference(form) === 0;
}

/** The default split: each part's share of the standalone prices (its part price when it has none); the last part takes the rounding rest so the sum is exactly 100. */
export function defaultAllocation(parts: readonly BuildPart[]): Record<string, string> {
  const weights = parts.map((part) => part.standalonePrice ?? part.partPrice);
  const total = weights.reduce((sum, weight) => sum + weight, 0);
  const percents = weights.map((weight) => (total > 0 ? round2((weight / total) * 100) : round2(100 / parts.length)));
  const last = percents.length - 1;
  if (last >= 0) percents[last] = round2(100 - percents.slice(0, last).reduce((sum, percent) => sum + percent, 0));
  return Object.fromEntries(parts.map((part, index) => [part.unitId, amountText(percents[index])]));
}

export const toAllocationInputs = (form: Readonly<Record<string, string>>): BuildAllocationInput[] =>
  Object.entries(form).map(([unitId, text]) => ({ unitId, percent: parseAmount(text) ?? 0 }));

/** The builds a part can move to or be taken from: every build except this one and the ones that are history. */
export const otherBuilds = (builds: readonly Build[], currentId: string): Build[] => builds.filter((build) => build.id !== currentId && !isLocked(build));

/** A build as a dropdown option: "BLD-07 — اسم التجميعة". */
export const buildOption = (build: Pick<Build, "id" | "code" | "name">) => ({ value: build.id, label: `${build.code} — ${build.name}` });

/** One typed part on the screen ("مش شرط تبقى في المخزون"): `key` keeps the inputs when a row is removed; `id` is "" for a new row. */
export interface PlannedRow {
  key: string;
  id: string;
  name: string;
  categoryId: string;
  brandId: string;
  price: string;
  /** The stock item of the same model it is linked to ("" = none) and its code for the chip. */
  stockItemId: string;
  stockItemCode: string;
}

export const emptyPlannedRow = (key: string): PlannedRow => ({ key, id: "", name: "", categoryId: "", brandId: "", price: "", stockItemId: "", stockItemCode: "" });

export const plannedRows = (parts: readonly PlannedPart[]): PlannedRow[] =>
  parts.map((part) => ({
    key: part.id, id: part.id, name: part.name, categoryId: part.categoryId ?? "", brandId: part.brandId ?? "", price: amountText(part.price),
    stockItemId: part.stockItemId ?? "", stockItemCode: part.stockItemCode ?? ""
  }));

/** Picking a model fills the name and, for a model in stock, its category and brand, and links the row to that item. */
export function pickPlannedModel(row: PlannedRow, suggestion: ModelSuggestion): PlannedRow {
  const template = suggestion.template;
  return {
    ...row,
    name: suggestion.modelName,
    categoryId: template?.categoryId ?? (row.categoryId || suggestion.categoryId || ""),
    brandId: template?.brandId ?? row.brandId,
    stockItemId: template?.itemId ?? "",
    stockItemCode: template?.code ?? ""
  };
}

/** Typing over a picked model unlinks the row from that stock item. */
export const typePlannedName = (row: PlannedRow, name: string): PlannedRow => ({ ...row, name, stockItemId: "", stockItemCode: "" });

export const plannedRowsValid = (rows: readonly PlannedRow[]): boolean => rows.every((row) => row.name.trim() !== "" && optionalAmountOk(row.price));

export const toPlannedInputs = (rows: readonly PlannedRow[]): PlannedPartInput[] =>
  rows.map((row) => ({
    id: row.id || null, name: row.name.trim(), categoryId: row.categoryId || null, brandId: row.brandId || null,
    price: parseAmount(row.price), stockItemId: row.stockItemId || null
  }));

/** The typed parts' prices that are filled in, summed. */
export const plannedTotal = (rows: readonly PlannedRow[]): number => round2(rows.reduce((sum, row) => sum + (parseAmount(row.price) || 0), 0));
