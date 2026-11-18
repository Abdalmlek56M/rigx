/**
 * Rules for the purchase intake screens: the form, its lines, the money side and the label keys. Pure, so it is tested.
 * The server stays the authority; these only keep the screen honest (the total, what is left to pay, what can be sent).
 * Messages live under `admin.purchases`.
 */
import { amountOk, amountText, optionalAmountOk, parseAmount } from "../stock/stock-fields.ts";
import type { Purchase, PurchaseLineRequest, PurchaseLineView, PurchaseRequest, Seller, SellerRequest, SellerSummary } from "./purchases-api";

const known = <T extends string>(list: readonly T[], value: string): T | "other" => list.find((entry) => entry === value) ?? "other";

export const PURCHASE_STATUSES = ["Draft", "Received", "Cancelled"] as const;
export const PURCHASE_KINDS = ["Parts", "FullPc"] as const;
export const PAYMENT_METHODS = ["Cash", "InstaPay", "Wallet", "TradeIn", "CashAndTradeIn"] as const;
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
  /** Kept as read when a draft is edited (a purchase converted from a request may carry specs). */
  specs: Record<string, string>;
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
  lines: LineForm[];
}

export function emptyLine(key: string): LineForm {
  return {
    key, categoryId: "", modelName: "", productId: "", condition: "Used", quantity: "1", serialNumber: "", unitCost: "", price: "",
    withBoardExtra: "", aloneExtra: "", partnerId: "", partnerPercent: "", isHidden: false, warrantyLabel: "", hasBox: false, internalNote: "", specs: {}
  };
}

export function emptyPurchase(today: string, firstLineKey: string): PurchaseForm {
  return {
    sellerId: "", kind: "Parts", keepAsBuild: false, buildName: "", purchaseDate: today, notes: "", paymentMethod: "Cash",
    paidAmount: "", tradeInValue: "", tradeInNote: "", lines: [emptyLine(firstLineKey)]
  };
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
    specs: line.specs
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
    lines: purchase.lines.map((line, index) => lineFromView(line, keyOf(index)))
  };
}

/** Used pieces are single pieces: the quantity is always 1; a new line's quantity is a whole number from 1 (0 when it is not). */
export function lineQuantity(line: LineForm): number {
  if (line.condition === "Used") return 1;
  const quantity = Number(line.quantity);
  return Number.isInteger(quantity) && quantity >= 1 ? quantity : 0;
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

export function lineValid(line: LineForm): boolean {
  return line.modelName.trim() !== "" && lineQuantity(line) > 0 && amountOk(line.unitCost) && amountOk(line.price)
    && optionalAmountOk(line.withBoardExtra) && optionalAmountOk(line.aloneExtra) && optionalAmountOk(line.partnerPercent);
}

/** Enough to send: a seller, valid lines (a whole PC has at least two), payment amounts that fit the method and the total. */
export function purchaseFormValid(form: PurchaseForm): boolean {
  if (!form.sellerId || form.lines.length === 0 || !form.lines.every(lineValid)) return false;
  if (form.kind === "FullPc" && form.lines.length < 2) return false;
  if (!optionalAmountOk(form.paidAmount) || !optionalAmountOk(form.tradeInValue) || isOverpaid(form)) return false;
  return !hasTradeIn(form.paymentMethod) || form.tradeInNote.trim() !== "";
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
    specs: line.specs
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
