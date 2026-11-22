/**
 * Rules for the purchase intake screens: the form, its lines, the money side and the label keys. Pure, so it is tested.
 * The server stays the authority; these only keep the screen honest (the total, what is left to pay, what can be sent).
 * Messages live under `admin.purchases`.
 */
import { amountOk, amountText, applySuggestion, editModel, optionalAmountOk, parseAmount } from "../stock/stock-fields.ts";
import type { Category } from "../categories/categories-api";
import { categoryPath } from "../inventory/inventory-rules.ts";
import type { StockItem } from "../inventory/inventory-api";
import type { ModelSuggestion, Purchase, PurchaseLineRequest, PurchaseLineView, PurchaseRequest, Seller, SellerRequest, SellerSummary } from "./purchases-api";

const known = <T extends string>(list: readonly T[], value: string): T | "other" => list.find((entry) => entry === value) ?? "other";

export const PURCHASE_STATUSES = ["Draft", "Received", "Cancelled"] as const;
export const PURCHASE_KINDS = ["Parts", "FullPc"] as const;
export const PAYMENT_METHODS = ["Cash", "InstaPay", "Wallet", "TradeIn", "CashAndTradeIn"] as const;
/** The methods a seller can be paid with later, after receiving (a trade-in only exists at the purchase). */
export const SELLER_PAYMENT_METHODS = ["Cash", "InstaPay", "Wallet"] as const;
export type SellerPaymentMethod = (typeof SELLER_PAYMENT_METHODS)[number];
export const SELLER_KINDS = ["Person", "Trader", "Company"] as const;
/** Where a seller found us (the board's list); stored as these words. */
export const SELLER_SOURCES = ["Website", "Facebook", "Marketplace", "Referral", "RegularTrader"] as const;

export type PaymentMethod = (typeof PAYMENT_METHODS)[number];

/** True for one of the board's sources (a seller saved with another word still shows it as typed). */
export const isKnownSource = (source: string): source is (typeof SELLER_SOURCES)[number] => SELLER_SOURCES.some((entry) => entry === source);

export const statusKey = (status: string) => known(PURCHASE_STATUSES, status);
export const kindKey = (kind: string) => known(PURCHASE_KINDS, kind);

/** Only a draft can still be edited, received or cancelled. */
export const isDraft = (purchase: Pick<Purchase, "status">) => purchase.status === "Draft";

/** The money paid in cash/InstaPay/wallet is possible for every method except a pure trade-in. */
export const hasMoney = (method: string) => method !== "TradeIn";
/** Trade-in value (and the note about what was taken) belongs to the two trade-in methods. */
export const hasTradeIn = (method: string) => method === "TradeIn" || method === "CashAndTradeIn";

export interface LineForm {
  /** Client-only id so the list keeps its inputs when a line is removed. */
  key: string;
  categoryId: string;
  modelName: string;
  productId: string;
  condition: "New" | "Used";
  quantity: string;
  serialNumber: string;
  unitCost: string;
  price: string;
  withBoardExtra: string;
  aloneExtra: string;
  partnerId: string;
  partnerPercent: string;
  isHidden: boolean;
  warrantyLabel: string;
  hasBox: boolean;
  internalNote: string;
  brandId: string;
  specs: Record<string, string>;
  /** "نسخ من منتج موجود": the stock item whose photos every received piece gets a copy of ("" = none). */
  copyPhotosFromItemId: string;
  /** What the copy chip says: the source item's code and photo count, when the line was filled on this screen. */
  copySource: { code: string; photoCount: number } | null;
}

export interface PurchaseForm {
  sellerId: string;
  kind: "Parts" | "FullPc";
  keepAsBuild: boolean;
  buildName: string;
  purchaseDate: string;
  notes: string;
  paymentMethod: PaymentMethod;
  paidAmount: string;
  tradeInValue: string;
  tradeInNote: string;
  /** false = old stock paid earlier or outside the store accounts: receiving it takes no money from them. */
  payFromCash: boolean;
  lines: LineForm[];
}

export function emptyLine(key: string): LineForm {
  return {
    key, categoryId: "", modelName: "", productId: "", condition: "Used", quantity: "1", serialNumber: "", unitCost: "", price: "",
    withBoardExtra: "", aloneExtra: "", partnerId: "", partnerPercent: "", isHidden: false, warrantyLabel: "", hasBox: false, internalNote: "", brandId: "",
    specs: {}, copyPhotosFromItemId: "", copySource: null
  };
}

/** A new purchase; `firstLine` is a line copied from an existing item ("نسخ لقطعة جديدة" in the inventory). */
export function emptyPurchase(today: string, firstLineKey: string, firstLine?: LineForm): PurchaseForm {
  return {
    sellerId: "", kind: "Parts", keepAsBuild: false, buildName: "", purchaseDate: today, notes: "", paymentMethod: "Cash",
    paidAmount: "", tradeInValue: "", tradeInNote: "", payFromCash: true, lines: [firstLine ?? emptyLine(firstLineKey)]
  };
}

/** Picking a model fills the line from it; a model in stock also has its photos copied on receiving (when it has any). */
export function pickModel(line: LineForm, suggestion: ModelSuggestion): LineForm {
  const template = suggestion.template;
  const copy = template && template.photoCount > 0 ? template : null;
  return {
    ...applySuggestion(line, suggestion),
    copyPhotosFromItemId: copy?.itemId ?? "",
    copySource: copy ? { code: copy.code, photoCount: copy.photoCount } : null
  };
}

/** Typing over a picked model makes it free text again: the other model's photos are no longer copied (the rest stays editable). */
export const typeModel = (line: LineForm, modelName: string): LineForm => ({ ...editModel(line, modelName), copyPhotosFromItemId: "", copySource: null });

/** Keeps the copied data but not the photos. */
export const dropPhotoCopy = (line: LineForm): LineForm => ({ ...line, copyPhotosFromItemId: "", copySource: null });

/** A line for one more piece like `item`: its model, category, brand, specs and photos; price, cost, condition and serial are left to fill. */
export function lineFromItem(item: StockItem, key: string): LineForm {
  return pickModel(emptyLine(key), {
    modelName: item.modelName, categoryId: item.categoryId, productId: item.productId, itemCount: 1,
    template: {
      itemId: item.id, code: item.code, modelName: item.modelName, categoryId: item.categoryId, brandId: item.brandId,
      productId: item.productId, specs: item.specs, photoCount: item.photos.length
    }
  });
}

export function lineFromView(line: PurchaseLineView, key: string): LineForm {
  return {
    key,
    categoryId: line.categoryId ?? "",
    modelName: line.modelName,
    productId: line.productId ?? "",
    condition: line.condition === "New" ? "New" : "Used",
    quantity: String(line.quantity),
    serialNumber: line.serialNumber ?? "",
    unitCost: amountText(line.unitCost),
    price: amountText(line.price),
    withBoardExtra: amountText(line.withBoardExtra),
    aloneExtra: amountText(line.aloneExtra),
    partnerId: line.partnerId ?? "",
    partnerPercent: amountText(line.partnerPercent),
    isHidden: line.isHidden,
    warrantyLabel: line.warrantyLabel ?? "",
    hasBox: line.hasBox,
    internalNote: line.internalNote ?? "",
    brandId: line.brandId ?? "",
    specs: line.specs,
    copyPhotosFromItemId: line.copyPhotosFromItemId ?? "",
    copySource: null
  };
}

export function purchaseFromView(purchase: Purchase, keyOf: (index: number) => string): PurchaseForm {
  const method = PAYMENT_METHODS.find((entry) => entry === purchase.paymentMethod) ?? "Cash";
  return {
    sellerId: purchase.seller.id,
    kind: purchase.kind === "FullPc" ? "FullPc" : "Parts",
    keepAsBuild: purchase.keepAsBuild,
    buildName: purchase.buildName ?? "",
    purchaseDate: purchase.purchaseDate,
    notes: purchase.notes ?? "",
    paymentMethod: method,
    paidAmount: purchase.paidAmount > 0 ? amountText(purchase.paidAmount) : "",
    tradeInValue: purchase.tradeInValue > 0 ? amountText(purchase.tradeInValue) : "",
    tradeInNote: purchase.tradeInNote ?? "",
    payFromCash: purchase.payFromCash,
    lines: purchase.lines.map((line, index) => lineFromView(line, keyOf(index)))
  };
}

/** Used pieces are single pieces: the quantity is always 1; a new line's quantity is a whole number from 1 (0 when it is not). */
export function lineQuantity(line: LineForm): number {
  if (line.condition === "Used") return 1;
  const quantity = Number(line.quantity);
  return Number.isInteger(quantity) && quantity >= 1 ? quantity : 0;
}

/** What one piece earns if it sells at the typed price: in pounds, and as a share of that price (the dashboard's definition of a margin). */
export interface LineMargin {
  amount: number;
  percent: number;
}

/** The margin shown beside the cost and the price while they are typed; `null` until both are numbers and the price is above zero. */
export function lineMargin(line: Pick<LineForm, "unitCost" | "price">): LineMargin | null {
  const cost = parseAmount(line.unitCost);
  const price = parseAmount(line.price);
  if (cost === null || price === null || !Number.isFinite(cost) || !Number.isFinite(price) || price <= 0) return null;
  const amount = Math.round((price - cost) * 100) / 100;
  return { amount, percent: Math.round(((price - cost) / price) * 1000) / 10 };
}

const serialKey = (serial: string): string => serial.replace(/\s+/g, "").toLowerCase();

/** The keys of the lines whose serial number is also typed on another line (a scanner that fired twice); case and spaces do not matter. */
export function repeatedSerialLines(lines: readonly Pick<LineForm, "key" | "serialNumber">[]): ReadonlySet<string> {
  const seen = new Map<string, string[]>();
  for (const line of lines) {
    const key = serialKey(line.serialNumber);
    if (key !== "") seen.set(key, [...(seen.get(key) ?? []), line.key]);
  }
  return new Set([...seen.values()].filter((keys) => keys.length > 1).flat());
}

export const lineTotal = (line: LineForm): number => lineQuantity(line) * (parseAmount(line.unitCost) || 0);

/** total = Σ quantity × unit cost. */
export const purchaseTotal = (lines: readonly LineForm[]): number => Math.round(lines.reduce((sum, line) => sum + lineTotal(line), 0) * 100) / 100;

/** What is still owed to the seller: total − paid − trade-in, never shown below 0. */
export function remainingAmount(total: number, paid: number, tradeIn: number): number {
  return Math.max(0, Math.round((total - paid - tradeIn) * 100) / 100);
}

const moneyOf = (text: string) => parseAmount(text) || 0;

/** Paid money plus the trade-in value are more than the total (the server refuses it; the form says why). */
export const isOverpaid = (form: PurchaseForm): boolean => moneyOf(form.paidAmount) + moneyOf(form.tradeInValue) > purchaseTotal(form.lines);

/** The payment fields that belong to the chosen method (the others are cleared). */
export function withMethod(form: PurchaseForm, paymentMethod: PaymentMethod): PurchaseForm {
  return {
    ...form,
    paymentMethod,
    paidAmount: hasMoney(paymentMethod) ? form.paidAmount : "",
    tradeInValue: hasTradeIn(paymentMethod) ? form.tradeInValue : "",
    tradeInNote: hasTradeIn(paymentMethod) ? form.tradeInNote : ""
  };
}

/** What is missing on the screen, in reading order; `label` is a key under `admin.purchases.form.missing`. */
export type MissingLabel =
  | "seller" | "category" | "model" | "condition" | "quantity" | "unitCost" | "price"
  | "withBoardExtra" | "aloneExtra" | "partnerPercent" | "fullPcParts" | "paidAmount" | "tradeInValue" | "tradeInNote" | "overpaid";

export interface MissingField {
  /** The field key (line fields match the input name after `line{index}-`; `lines`/`sellerId` are section markers). */
  field: string;
  line: number | null;
  label: MissingLabel;
}

/** The card title: the model, else the category path, else the caller's fallback ("القطعة 2"). */
export function lineDisplayName(line: LineForm, tree: readonly Category[], fallback: string): string {
  const model = line.modelName.trim();
  if (model) return model;
  return categoryPath(tree, line.categoryId) || fallback;
}

/**
 * Seller, each line's inputs, the whole-PC part count, then payment. Empty when the form can be sent.
 * A used piece's serial stays optional (not every used part has a readable one), as on the server.
 */
export function missingFields(form: PurchaseForm): MissingField[] {
  const missing: MissingField[] = [];
  if (!form.sellerId) missing.push({ field: "sellerId", line: null, label: "seller" });

  form.lines.forEach((line, index) => {
    if (!line.categoryId) missing.push({ field: "categoryId", line: index, label: "category" });
    if (!line.modelName.trim()) missing.push({ field: "modelName", line: index, label: "model" });
    if (line.condition !== "New" && line.condition !== "Used")
      missing.push({ field: "condition", line: index, label: "condition" });
    if (line.condition === "New" && lineQuantity(line) === 0)
      missing.push({ field: "quantity", line: index, label: "quantity" });
    if (!amountOk(line.unitCost)) missing.push({ field: "unitCost", line: index, label: "unitCost" });
    if (!amountOk(line.price)) missing.push({ field: "price", line: index, label: "price" });
    if (!optionalAmountOk(line.withBoardExtra)) missing.push({ field: "withBoardExtra", line: index, label: "withBoardExtra" });
    if (!optionalAmountOk(line.aloneExtra)) missing.push({ field: "aloneExtra", line: index, label: "aloneExtra" });
    if (!optionalAmountOk(line.partnerPercent)) missing.push({ field: "partnerPercent", line: index, label: "partnerPercent" });
  });

  if (form.kind === "FullPc" && form.lines.length < 2)
    missing.push({ field: "lines", line: null, label: "fullPcParts" });

  if (!optionalAmountOk(form.paidAmount)) missing.push({ field: "paidAmount", line: null, label: "paidAmount" });
  if (!optionalAmountOk(form.tradeInValue)) missing.push({ field: "tradeInValue", line: null, label: "tradeInValue" });
  if (hasTradeIn(form.paymentMethod) && !form.tradeInNote.trim())
    missing.push({ field: "tradeInNote", line: null, label: "tradeInNote" });
  if (isOverpaid(form)) missing.push({ field: "paidAmount", line: null, label: "overpaid" });

  return missing;
}

export function toLineRequest(line: LineForm): PurchaseLineRequest {
  return {
    modelName: line.modelName.trim(),
    condition: line.condition,
    quantity: lineQuantity(line),
    unitCost: parseAmount(line.unitCost) ?? 0,
    price: parseAmount(line.price) ?? 0,
    categoryId: line.categoryId || null,
    productId: line.productId || null,
    withBoardExtra: parseAmount(line.withBoardExtra),
    aloneExtra: parseAmount(line.aloneExtra),
    serialNumber: line.serialNumber.trim() || null,
    partnerId: line.partnerId || null,
    partnerPercent: line.partnerId ? parseAmount(line.partnerPercent) : null,
    isHidden: line.isHidden,
    warrantyLabel: line.warrantyLabel.trim() || null,
    hasBox: line.hasBox,
    internalNote: line.internalNote.trim() || null,
    specs: line.specs,
    brandId: line.brandId || null,
    copyPhotosFromItemId: line.copyPhotosFromItemId || null
  };
}

/** The request for a new draft, or (with `rowVersion`) for saving an existing one. */
export function toPurchaseRequest(form: PurchaseForm, rowVersion?: number): PurchaseRequest {
  const fullPc = form.kind === "FullPc";
  return {
    sellerId: form.sellerId,
    kind: form.kind,
    keepAsBuild: fullPc && form.keepAsBuild,
    buildName: fullPc && form.keepAsBuild ? form.buildName.trim() || null : null,
    purchaseDate: form.purchaseDate || null,
    notes: form.notes.trim() || null,
    paymentMethod: form.paymentMethod,
    paidAmount: hasMoney(form.paymentMethod) ? moneyOf(form.paidAmount) : 0,
    tradeInValue: hasTradeIn(form.paymentMethod) ? moneyOf(form.tradeInValue) : 0,
    tradeInNote: hasTradeIn(form.paymentMethod) ? form.tradeInNote.trim() || null : null,
    payFromCash: form.payFromCash,
    lines: form.lines.map(toLineRequest),
    rowVersion
  };
}

export interface SellerForm {
  kind: (typeof SELLER_KINDS)[number];
  name: string;
  phone: string;
  nationalId: string;
  governorate: string;
  area: string;
  source: string;
  notes: string;
}

export const emptySeller = (phone: string): SellerForm => ({ kind: "Person", name: "", phone, nationalId: "", governorate: "", area: "", source: "", notes: "" });

export const sellerFromView = (seller: Seller): SellerForm => ({
  kind: SELLER_KINDS.find((entry) => entry === seller.kind) ?? "Person",
  name: seller.name,
  phone: seller.phone,
  nationalId: seller.nationalId ?? "",
  governorate: seller.governorate ?? "",
  area: seller.area ?? "",
  source: seller.source ?? "",
  notes: seller.notes ?? ""
});

export const sellerFormValid = (form: SellerForm): boolean => form.name.trim() !== "" && form.phone.trim() !== "";

export const toSellerRequest = (form: SellerForm, rowVersion?: number): SellerRequest => ({
  kind: form.kind,
  name: form.name.trim(),
  phone: form.phone.trim(),
  nationalId: form.nationalId.trim() || null,
  governorate: form.governorate || null,
  area: form.area.trim() || null,
  source: form.source || null,
  notes: form.notes.trim() || null,
  rowVersion
});

export const toSummary = (seller: Seller): SellerSummary => ({ id: seller.id, kind: seller.kind, name: seller.name, phone: seller.phone });

/** Digits, spaces, dashes and a leading plus: a phone number rather than a name. */
export const looksLikePhone = (text: string): boolean => /^\+?[\d\s-]+$/.test(text.trim());

/** The seller lookup parameters for what was typed: by phone when it is one, else by name/phone text. */
export const sellerQuery = (text: string): { phone?: string; q?: string } => (looksLikePhone(text) ? { phone: text.trim() } : { q: text.trim() });
