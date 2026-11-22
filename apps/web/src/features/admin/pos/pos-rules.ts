/**
 * ADM-POS: the cart, the customer/fulfillment/payment form and the sale request the API expects. The sale price is typed per
 * line (business §5); the server prices, checks the discount limit and stock, and decides. Pure, so it is tested.
 */
import type { Schemas } from "@rigx/api-client";
import type { StorePaymentMethod } from "../orders/order-rules.ts";

export const PRICE_OPTIONS = ["Alone", "WithBoard", "InBuild"] as const;
export type PriceOption = (typeof PRICE_OPTIONS)[number];

export const POS_USAGES = ["Gaming", "StudyWork", "Editing", "Streaming", "Upgrade", "Other"] as const;
export type PosUsage = (typeof POS_USAGES)[number];

/** A stock item found by the search: `stockItemId` when it came from the inventory, else it is sent by its code. */
export interface ItemPick {
  stockItemId: string | null;
  code: string;
  name: string;
  condition: string;
  warrantyLabel: string | null;
  prices: { alone: number; withBoard: number; inBuild: number };
  available: number;
  /** The sale prices the item's category offers, in the order shown (the others are hidden and cannot be picked). */
  priceOptions: readonly PriceOption[];
}

export interface BuildPart {
  unitId: string;
  name: string;
  code: string;
  condition: string;
  warrantyLabel: string | null;
  partPrice: number;
}

/** A build to sell; `parts` is null when they are not known (sold whole only). */
export interface BuildPick {
  assemblyId: string;
  code: string;
  name: string;
  salePrice: number;
  splittable: boolean;
  parts: BuildPart[] | null;
}

/** The known price options named in `names`, in the usual order; none named (an old answer) means all three. */
export function offeredOptions(names: readonly string[] | null | undefined): readonly PriceOption[] {
  const offered = PRICE_OPTIONS.filter((option) => names?.includes(option));
  return offered.length > 0 ? offered : PRICE_OPTIONS;
}

/** Store rows carry their offered prices; inventory rows get them from their category (`optionsOf`). */
export function toItemPick(
  row: Schemas["StockItemAdminView"] | Schemas["StoreItemCard"],
  optionsOf: (categoryId: string | null) => readonly PriceOption[] = () => PRICE_OPTIONS
): ItemPick {
  const admin = "id" in row;
  return {
    stockItemId: admin ? row.id : null,
    code: row.code,
    name: row.modelName,
    condition: row.condition,
    warrantyLabel: row.warrantyLabel,
    prices: row.prices,
    available: admin ? row.availableCount : row.available,
    priceOptions: admin ? optionsOf(row.categoryId) : offeredOptions(row.priceOptions)
  };
}

export function toBuildPick(row: Schemas["AssemblyView"] | Schemas["AssemblyPublicView"]): BuildPick {
  const admin = "name" in row;
  const name = admin ? row.publicTitleAr ?? row.publicTitleEn ?? row.name : row.titleAr ?? row.titleEn ?? row.code;
  const parts = admin
    ? row.units.map((unit): BuildPart => ({
        unitId: unit.unitId, name: unit.name, code: unit.stockItemCode, condition: unit.condition,
        warrantyLabel: unit.warrantyLabel, partPrice: unit.partPrice
      }))
    : null;
  return {
    assemblyId: row.id, code: row.code, name, salePrice: row.salePrice,
    splittable: row.sellMode === "FullOrSplit" && parts !== null, parts
  };
}

export interface ItemLine {
  kind: "item";
  key: string;
  pick: ItemPick;
  priceOption: PriceOption;
  quantity: number;
  /** Per piece, as typed; starts at the shown price. */
  salePrice: string;
}

export interface BuildLine {
  kind: "build";
  key: string;
  pick: BuildPick;
  /** Whole build: one sale price. Split: the chosen parts, each with its own sale price. */
  mode: "whole" | "split";
  salePrice: string;
  parts: Record<string, { selected: boolean; salePrice: string }>;
}

export type CartLine = ItemLine | BuildLine;

export function shownPrice(pick: ItemPick, option: PriceOption): number {
  return option === "WithBoard" ? pick.prices.withBoard : option === "InBuild" ? pick.prices.inBuild : pick.prices.alone;
}

const money = (value: number) => String(value);

export function itemLine(pick: ItemPick): ItemLine {
  const priceOption = pick.priceOptions[0] ?? "Alone";
  return { kind: "item", key: `item:${pick.code}`, pick, priceOption, quantity: 1, salePrice: money(shownPrice(pick, priceOption)) };
}

export function buildLine(pick: BuildPick): BuildLine {
  const parts = Object.fromEntries((pick.parts ?? []).map((part) => [part.unitId, { selected: false, salePrice: money(part.partPrice) }]));
  return { kind: "build", key: `build:${pick.code}`, pick, mode: "whole", salePrice: money(pick.salePrice), parts };
}

/**
 * What Enter adds from the item search: the item whose code is exactly what was typed (a scanned or typed code), or the only item found;
 * `null` when there are several candidates (the cashier picks one) or none.
 */
export function soleMatch(items: readonly ItemPick[], query: string): ItemPick | null {
  const code = query.trim().toLowerCase();
  const exact = items.filter((pick) => pick.code.toLowerCase() === code);
  if (exact.length === 1) return exact[0] ?? null;
  return items.length === 1 ? items[0] ?? null : null;
}

/** Adds a pick, or one more piece of an item already in the cart (up to what is available); a build is added once. */
export function addToCart(cart: readonly CartLine[], line: CartLine): CartLine[] {
  const existing = cart.find((entry) => entry.key === line.key);
  if (!existing) return [...cart, line];
  if (existing.kind !== "item") return [...cart];
  return cart.map((entry) => entry === existing && entry.kind === "item"
    ? { ...entry, quantity: Math.min(entry.quantity + 1, entry.pick.available) }
    : entry);
}

/** A money field: empty, or zero or more with at most 2 decimals. */
export function moneyValid(text: string): boolean {
  if (text.trim() === "") return false;
  const value = Number(text);
  return Number.isFinite(value) && value >= 0 && Math.round(value * 100) === value * 100;
}

const toMoney = (text: string): number | null => (moneyValid(text) ? Number(text) : null);

export function selectedParts(line: BuildLine): BuildPart[] {
  return (line.pick.parts ?? []).filter((part) => line.parts[part.unitId]?.selected);
}

/** One API line per cart line; a split build sends its chosen parts. */
export function toOrderLine(line: CartLine): Schemas["OrderLineRequest"] {
  if (line.kind === "item") {
    return {
      stockItemId: line.pick.stockItemId, assemblyId: null, code: line.pick.stockItemId ? null : line.pick.code,
      quantity: line.quantity, priceOption: line.priceOption, salePrice: toMoney(line.salePrice), parts: null
    };
  }
  const split = line.mode === "split";
  return {
    stockItemId: null, assemblyId: line.pick.assemblyId, code: null, quantity: 1, priceOption: null,
    salePrice: split ? null : toMoney(line.salePrice),
    parts: split ? selectedParts(line).map((part) => ({ unitId: part.unitId, salePrice: toMoney(line.parts[part.unitId]!.salePrice) })) : null
  };
}

/** Lines the API can price: a split build needs at least one part. */
export function sendableLines(cart: readonly CartLine[]): CartLine[] {
  return cart.filter((line) => line.kind === "item" || line.mode === "whole" || selectedParts(line).length > 0);
}

export interface PaymentRow {
  key: string;
  method: StorePaymentMethod;
  amount: string;
}

export interface PosForm {
  name: string;
  phone: string;
  phoneHasWhatsApp: boolean;
  email: string;
  fulfillment: "Pickup" | "Delivery";
  governorate: string;
  area: string;
  street: string;
  building: string;
  landmark: string;
  /** Empty: the fee from the shipping settings for the governorate. */
  deliveryFee: string;
  discount: string;
  /** A coupon code to apply after the staff discount; empty for none. */
  couponCode: string;
  leadSource: string | null;
  usage: PosUsage | null;
  usageNote: string;
  notes: string;
  payments: PaymentRow[];
}

export const EMPTY_POS_FORM: PosForm = {
  name: "", phone: "", phoneHasWhatsApp: true, email: "", fulfillment: "Pickup", governorate: "", area: "", street: "",
  building: "", landmark: "", deliveryFee: "", discount: "", couponCode: "", leadSource: null, usage: null, usageNote: "", notes: "",
  payments: [{ key: "p0", method: "Cash", amount: "" }]
};

const orNull = (value: string): string | null => value.trim() || null;

/** Digits only, +20 / 0020 read as a local Egyptian mobile; the server accepts 8–15 digits. */
export function posPhone(value: string): string {
  const digits = value.replace(/\D/g, "");
  return digits.replace(/^(?:00)?20(?=1\d{9}$)/, "0");
}

export const posPhoneValid = (value: string) => posPhone(value).length >= 8 && posPhone(value).length <= 15;

/** Payment rows with an amount; empty rows are ignored. */
export function filledPayments(payments: readonly PaymentRow[]): PaymentRow[] {
  return payments.filter((row) => row.amount.trim() !== "");
}

export function paidTotal(payments: readonly PaymentRow[]): number {
  return Math.round(filledPayments(payments).reduce((sum, row) => sum + (toMoney(row.amount) ?? 0), 0) * 100) / 100;
}

export function toSaleRequest(cart: readonly CartLine[], form: PosForm, confirm: boolean): Schemas["PosSaleRequest"] {
  const delivery = form.fulfillment === "Delivery";
  return {
    customer: { name: form.name.trim(), phone: posPhone(form.phone), phoneHasWhatsApp: form.phoneHasWhatsApp, email: orNull(form.email) },
    fulfillmentMethod: form.fulfillment,
    address: delivery
      ? { governorate: form.governorate, area: form.area.trim(), street: form.street.trim(), building: orNull(form.building), landmark: orNull(form.landmark) }
      : null,
    items: sendableLines(cart).map(toOrderLine),
    discount: toMoney(form.discount) ?? 0,
    couponCode: orNull(form.couponCode.toUpperCase()),
    deliveryFee: delivery ? toMoney(form.deliveryFee) : null,
    payments: filledPayments(form.payments).map((row) => ({ method: row.method, amount: toMoney(row.amount) ?? 0, moneyAccountId: null })),
    leadSource: form.leadSource,
    usage: form.usage,
    usageNote: form.usage === "Other" ? orNull(form.usageNote) : null,
    notes: orNull(form.notes),
    confirm
  };
}

export type PosField =
  | "lines" | "salePrice" | "parts" | "name" | "phone" | "email" | "governorate" | "area" | "street" | "deliveryFee" | "discount"
  | "usageNote" | "payments" | "overpaid";

/** Client-side checks before selling (the server checks again, including stock, the discount limit and the blacklist). */
export function validateSale(cart: readonly CartLine[], form: PosForm, total: number | null): PosField[] {
  const found: PosField[] = [];
  if (cart.length === 0) found.push("lines");
  const prices = cart.flatMap((line) => line.kind === "item" || line.mode === "whole"
    ? [line.salePrice]
    : selectedParts(line).map((part) => line.parts[part.unitId]!.salePrice));
  if (prices.some((price) => !moneyValid(price))) found.push("salePrice");
  if (cart.some((line) => line.kind === "build" && line.mode === "split" && selectedParts(line).length === 0)) found.push("parts");
  if (!form.name.trim()) found.push("name");
  if (!posPhoneValid(form.phone)) found.push("phone");
  if (form.email.trim() && !/^[^\s@]+@[^\s@]+$/.test(form.email.trim())) found.push("email");
  if (form.fulfillment === "Delivery") {
    if (!form.governorate) found.push("governorate");
    if (!form.area.trim()) found.push("area");
    if (!form.street.trim()) found.push("street");
    if (form.deliveryFee.trim() && !moneyValid(form.deliveryFee)) found.push("deliveryFee");
  }
  if (form.discount.trim() && !moneyValid(form.discount)) found.push("discount");
  if (form.usage === "Other" && !form.usageNote.trim()) found.push("usageNote");
  const payments = filledPayments(form.payments);
  if (payments.some((row) => !moneyValid(row.amount) || Number(row.amount) === 0)) found.push("payments");
  else if (total !== null && paidTotal(payments) > total) found.push("overpaid");
  return found;
}

/** When the paid amount is the only thing missing, the first payment row takes what is left of the total. */
export function fillRemaining(payments: readonly PaymentRow[], total: number): PaymentRow[] {
  const others = paidTotal(payments.slice(1));
  const left = Math.max(0, Math.round((total - others) * 100) / 100);
  return payments.map((row, index) => (index === 0 ? { ...row, amount: left > 0 ? String(left) : "" } : row));
}

/** The server's customer search needs 4 digits of a phone or 2 letters of a name (same rule as the API's `PosCustomerQuery`). */
export function customerSearchReady(text: string): boolean {
  const value = text.trim();
  return /^[\d\s+-]+$/.test(value) ? value.replace(/\D/g, "").length >= 4 : value.length >= 2;
}

/** Picking a past buyer: the phone and its WhatsApp flag become theirs; their other details fill only what the cashier left empty. */
export function withCustomer(form: PosForm, customer: Schemas["PosCustomerView"]): PosForm {
  const keep = (current: string, known: string | null | undefined) => (current.trim() || !known ? current : known);
  const address = customer.address;
  return {
    ...form,
    phone: customer.phone,
    phoneHasWhatsApp: customer.phoneHasWhatsApp,
    name: keep(form.name, customer.name),
    email: keep(form.email, customer.email),
    governorate: keep(form.governorate, address?.governorate),
    area: keep(form.area, address?.area),
    street: keep(form.street, address?.street),
    building: keep(form.building, address?.building),
    landmark: keep(form.landmark, address?.landmark)
  };
}

export type PaymentState =
  | { kind: "unpriced" }
  | { kind: "due"; amount: number }
  | { kind: "settled" }
  | { kind: "over"; amount: number };

/** Where the payment stands against the server's total: nothing to compare yet, money still due, paid in full, or paid too much. */
export function paymentState(total: number | null, paid: number): PaymentState {
  if (total === null) return { kind: "unpriced" };
  const left = Math.round((total - paid) * 100) / 100;
  if (left > 0) return { kind: "due", amount: left };
  return left === 0 ? { kind: "settled" } : { kind: "over", amount: -left };
}
