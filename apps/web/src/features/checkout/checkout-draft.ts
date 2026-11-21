import {
  CHECKOUT_USAGES, EMPTY_FORM, type CheckoutFormState, type CheckoutUsage, type FulfillmentMethod
} from "./checkout-request.ts";

/** Versioned so an old draft with a different shape is ignored, not half-read. */
export const CHECKOUT_DRAFT_KEY = "rigx.checkout.draft";
export const CHECKOUT_DRAFT_VERSION = 1;

/** Only what the customer typed in the order form; no coupon result, payment status or anything sensitive. */
export type CheckoutDraft = Partial<Pick<CheckoutFormState,
  "name" | "phone" | "phoneHasWhatsApp" | "whatsAppNumber" | "email" | "emailConfirm" | "fulfillment" | "governorate" | "area" | "street" |
  "building" | "landmark" | "notes" | "usage" | "usageNote" | "leadSource">>;

const TEXT_FIELDS = ["name", "phone", "whatsAppNumber", "email", "emailConfirm", "governorate", "area", "street", "building", "landmark", "notes", "usageNote"] as const;

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === "object" && value !== null && !Array.isArray(value);
const isText = (value: unknown): value is string => typeof value === "string";
const isFulfillment = (value: unknown): value is FulfillmentMethod => value === "Delivery" || value === "Pickup";
const isUsage = (value: unknown): value is CheckoutUsage => CHECKOUT_USAGES.some((usage) => usage === value);

/** The saved part of a form (fresh object, so the live form is never stored by reference). */
export function toDraft(form: CheckoutFormState): CheckoutDraft {
  return {
    name: form.name, phone: form.phone, phoneHasWhatsApp: form.phoneHasWhatsApp, whatsAppNumber: form.whatsAppNumber,
    email: form.email, emailConfirm: form.emailConfirm,
    fulfillment: form.fulfillment, governorate: form.governorate, area: form.area, street: form.street, building: form.building,
    landmark: form.landmark, notes: form.notes, usage: form.usage, usageNote: form.usageNote, leadSource: form.leadSource
  };
}

/**
 * Reads a stored draft. Bad JSON, another version, or a value the form cannot hold is ignored for that field; anything
 * unreadable gives `null` and the page starts empty.
 */
export function parseDraft(raw: string | null): CheckoutDraft | null {
  if (!raw) return null;
  try {
    const value: unknown = JSON.parse(raw);
    if (!isRecord(value) || value.version !== CHECKOUT_DRAFT_VERSION || !isRecord(value.form)) return null;
    const form = value.form;
    const draft: CheckoutDraft = {};
    for (const field of TEXT_FIELDS) {
      if (isText(form[field])) draft[field] = form[field];
    }
    if (typeof form.phoneHasWhatsApp === "boolean") draft.phoneHasWhatsApp = form.phoneHasWhatsApp;
    if (isFulfillment(form.fulfillment)) draft.fulfillment = form.fulfillment;
    if (form.usage === null) draft.usage = null;
    else if (isUsage(form.usage)) draft.usage = form.usage;
    if (form.leadSource === null) draft.leadSource = null;
    else if (isText(form.leadSource)) draft.leadSource = form.leadSource;
    return draft;
  } catch {
    return null;
  }
}

/** A full form from a partial draft; absent or invalid fields keep their empty default. */
export function mergeDraft(draft: CheckoutDraft | null): CheckoutFormState {
  return draft ? { ...EMPTY_FORM, ...draft } : EMPTY_FORM;
}

/** The text written to storage for a form. */
export function draftJson(form: CheckoutFormState): string {
  return JSON.stringify({ version: CHECKOUT_DRAFT_VERSION, form: toDraft(form) });
}

/** The draft left by an earlier visit, or null when storage is unavailable or empty. */
export function readCheckoutDraft(): CheckoutDraft | null {
  try {
    return parseDraft(window.localStorage.getItem(CHECKOUT_DRAFT_KEY));
  } catch {
    return null;
  }
}

export function saveCheckoutDraft(form: CheckoutFormState): void {
  try {
    window.localStorage.setItem(CHECKOUT_DRAFT_KEY, draftJson(form));
  } catch {
    // Storage may be full or disabled; checkout keeps working without it.
  }
}

export function clearCheckoutDraft(): void {
  try {
    window.localStorage.removeItem(CHECKOUT_DRAFT_KEY);
  } catch {
    // Nothing to clear when storage is unavailable.
  }
}
