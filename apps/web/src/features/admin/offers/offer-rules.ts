/** Offers: status labels, Cairo date ⇄ UTC, form ⇄ API and discount math. Pure, so it is tested. */
import { addDays } from "../dashboard/dashboard-rules.ts";
import type { Offer, OfferInput, OfferItemView, ProductSummary } from "./offers-api";

export const OFFER_STATUSES = ["Draft", "Published", "Cancelled"] as const;
export type OfferStatusKey = (typeof OFFER_STATUSES)[number] | "Other";

export function offerStatusKey(status: string): OfferStatusKey {
  return OFFER_STATUSES.find((known) => known === status) ?? "Other";
}

/** The Cairo calendar date (`YYYY-MM-DD`) of an instant. */
export function cairoDate(iso: string): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Africa/Cairo", year: "numeric", month: "2-digit", day: "2-digit" })
    .format(new Date(iso));
}

/** The Cairo UTC offset at a date's local midnight (Egypt has DST again since 2023). */
function cairoOffsetMs(date: string): number {
  const utc = new Date(`${date}T00:00:00Z`);
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Africa/Cairo", year: "numeric", month: "2-digit", day: "2-digit",
    hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23"
  }).formatToParts(utc);
  const part = (type: string) => Number(parts.find((entry) => entry.type === type)?.value ?? 0);
  return Date.UTC(part("year"), part("month") - 1, part("day"), part("hour"), part("minute"), part("second")) - utc.getTime();
}

/** 00:00 Cairo on that day, as a UTC ISO string. */
export function cairoDayStartUtc(date: string): string {
  return new Date(Date.parse(`${date}T00:00:00Z`) - cairoOffsetMs(date)).toISOString();
}

/** 23:59:59.999 Cairo on that day, as a UTC ISO string (an offer's end date includes its last day). */
export function cairoDayEndUtc(date: string): string {
  return new Date(Date.parse(cairoDayStartUtc(addDays(date, 1))) - 1).toISOString();
}

export interface OfferItemForm {
  productId: string;
  name: string;
  sku: string;
  basePrice: number;
  offerPrice: string;
}

export interface OfferForm {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  startDate: string;
  endDate: string;
  priority: string;
  items: OfferItemForm[];
}

export function emptyOfferForm(today: string): OfferForm {
  return { titleAr: "", titleEn: "", descriptionAr: "", descriptionEn: "", startDate: today, endDate: "", priority: "0", items: [] };
}

export function offerForm(offer: Offer): OfferForm {
  return {
    titleAr: offer.titleAr,
    titleEn: offer.titleEn,
    descriptionAr: offer.descriptionAr ?? "",
    descriptionEn: offer.descriptionEn ?? "",
    startDate: cairoDate(offer.startsAtUtc),
    endDate: offer.endsAtUtc ? cairoDate(offer.endsAtUtc) : "",
    priority: String(offer.priority),
    items: offer.items.map((item) => ({
      productId: item.productId, name: item.productName, sku: item.productSku, basePrice: item.basePrice,
      offerPrice: String(item.offerPrice)
    }))
  };
}

export function toOfferInput(form: OfferForm): OfferInput {
  const priority = Number.parseInt(form.priority, 10);
  return {
    titleAr: form.titleAr.trim(),
    titleEn: form.titleEn.trim(),
    descriptionAr: form.descriptionAr.trim() === "" ? null : form.descriptionAr.trim(),
    descriptionEn: form.descriptionEn.trim() === "" ? null : form.descriptionEn.trim(),
    startsAtUtc: form.startDate === "" ? null : cairoDayStartUtc(form.startDate),
    endsAtUtc: form.endDate === "" ? null : cairoDayEndUtc(form.endDate),
    priority: Number.isFinite(priority) ? priority : 0,
    items: form.items.map((item) => ({ productId: item.productId, offerPrice: Number(item.offerPrice) }))
  };
}

/** An end date set before the start date (shown on the end-date field; the save stays off). */
export function endsBeforeStart(form: Pick<OfferForm, "startDate" | "endDate">): boolean {
  return form.endDate !== "" && form.endDate < form.startDate;
}

export function offerValid(form: OfferForm): boolean {
  if (form.titleAr.trim() === "" || form.titleEn.trim() === "") return false;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(form.startDate)) return false;
  if (form.endDate !== "" && (endsBeforeStart(form) || !/^\d{4}-\d{2}-\d{2}$/.test(form.endDate))) return false;
  if (form.items.length === 0) return false;
  return form.items.every((item) => {
    const price = Number(item.offerPrice);
    return item.offerPrice.trim() !== "" && Number.isFinite(price) && price > 0 && price < item.basePrice;
  });
}

/** The discount of one item, as a percentage with one decimal. */
export function itemDiscount(basePrice: number, offerPrice: number): number {
  if (basePrice <= 0) return 0;
  return Math.round((1 - offerPrice / basePrice) * 1000) / 10;
}

/** The lowest and highest discount in an offer (null when it has no items). */
export function offerDiscount(items: readonly Pick<OfferItemView, "basePrice" | "offerPrice">[]): { min: number; max: number } | null {
  if (items.length === 0) return null;
  const values = items.map((item) => itemDiscount(item.basePrice, item.offerPrice));
  return { min: Math.min(...values), max: Math.max(...values) };
}

/** The products that match the search and are not in the offer yet (at most eight). */
export function filterProducts(products: readonly ProductSummary[], search: string, chosen: readonly string[]): ProductSummary[] {
  const query = search.trim().toLowerCase();
  return products
    .filter((product) => product.status === "Published")
    .filter((product) => !chosen.includes(product.id))
    .filter((product) => query === "" || `${product.name} ${product.sku} ${product.slug}`.toLowerCase().includes(query))
    .slice(0, 8);
}

export function addOfferItem(items: readonly OfferItemForm[], product: ProductSummary): OfferItemForm[] {
  if (items.some((item) => item.productId === product.id)) return [...items];
  return [...items, { productId: product.id, name: product.name, sku: product.sku, basePrice: product.basePrice, offerPrice: (product.basePrice * 0.9).toFixed(2) }];
}

export function removeOfferItem(items: readonly OfferItemForm[], productId: string): OfferItemForm[] {
  return items.filter((item) => item.productId !== productId);
}
