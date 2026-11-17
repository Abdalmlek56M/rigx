import type { Schemas } from "@rigx/api-client";
import { isEgyptMobile, normalizePhone } from "../requests/request-fields.ts";
import type { CartLine } from "../cart/cart-rules.ts";

export type FulfillmentMethod = "Delivery" | "Pickup";
export type PaymentMethod = "CashOnDelivery" | "InstaPay" | "Wallet";

export const CHECKOUT_USAGES = ["Gaming", "StudyWork", "Editing", "Streaming", "Upgrade", "Other"] as const;
export type CheckoutUsage = (typeof CHECKOUT_USAGES)[number];

export interface CheckoutFormState {
  name: string;
  phone: string;
  phoneHasWhatsApp: boolean;
  email: string;
  fulfillment: FulfillmentMethod;
  governorate: string;
  area: string;
  street: string;
  building: string;
  landmark: string;
  notes: string;
  usage: CheckoutUsage | null;
  usageNote: string;
  leadSource: string | null;
  paymentMethod: PaymentMethod | null;
  termsAccepted: boolean;
}

export const EMPTY_FORM: CheckoutFormState = {
  name: "", phone: "", phoneHasWhatsApp: true, email: "", fulfillment: "Delivery", governorate: "", area: "", street: "",
  building: "", landmark: "", notes: "", usage: null, usageNote: "", leadSource: null, paymentMethod: null, termsAccepted: false
};

/** Fields the form can flag; the message for each lives under `checkout.errors.<key>`. */
export type FieldKey = "name" | "phone" | "governorate" | "area" | "street" | "usageNote" | "paymentMethod" | "terms";

/** The order lines the API expects: public codes only. A build sells one copy per line, so N copies become N lines. */
export function toOrderLines(lines: CartLine[]): Schemas["OrderLineRequest"][] {
  return lines.flatMap((line): Schemas["OrderLineRequest"][] => {
    const base = { code: line.code, stockItemId: null, assemblyId: null, salePrice: null, parts: null };
    if (line.kind === "Build") return Array.from({ length: line.quantity }, () => ({ ...base, quantity: 1, priceOption: null }));
    return [{ ...base, quantity: line.quantity, priceOption: line.priceOption }];
  });
}

const orNull = (value: string): string | null => value.trim() || null;

export function toCheckoutRequest(form: CheckoutFormState, lines: CartLine[]): Schemas["CheckoutRequest"] {
  const delivery = form.fulfillment === "Delivery";
  return {
    customer: { name: form.name.trim(), phone: normalizePhone(form.phone), phoneHasWhatsApp: form.phoneHasWhatsApp, email: orNull(form.email) },
    fulfillmentMethod: form.fulfillment,
    address: delivery
      ? {
          governorate: form.governorate, area: form.area.trim(), street: form.street.trim(),
          building: orNull(form.building), landmark: orNull(form.landmark)
        }
      : null,
    paymentMethod: form.paymentMethod,
    items: toOrderLines(lines),
    notes: orNull(form.notes),
    usage: form.usage,
    usageNote: form.usage === "Other" ? orNull(form.usageNote) : null,
    leadSource: form.leadSource,
    termsAccepted: form.termsAccepted
  };
}

/** Client-side checks before sending; the server validates again. */
export function validateCheckout(form: CheckoutFormState): FieldKey[] {
  const found: FieldKey[] = [];
  if (!form.name.trim()) found.push("name");
  if (!isEgyptMobile(form.phone)) found.push("phone");
  if (form.fulfillment === "Delivery") {
    if (!form.governorate) found.push("governorate");
    if (!form.area.trim()) found.push("area");
    if (!form.street.trim()) found.push("street");
  }
  if (form.usage === "Other" && !form.usageNote.trim()) found.push("usageNote");
  if (!form.paymentMethod) found.push("paymentMethod");
  if (!form.termsAccepted) found.push("terms");
  return found;
}
