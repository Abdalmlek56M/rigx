import type { APIRequestContext } from "@playwright/test";
import { getJson, postJson } from "./api";
import { uniqueName, uniquePhone } from "./names";

export interface StockPick {
  code: string;
  modelName: string;
}

interface StorePage {
  items: (StockPick & { condition: string; available: number })[];
}

/**
 * A new part with stock, found through the public catalog (never a fixed demo id). `offset` lets the specs take different parts, so one
 * flow does not sell what another is about to use.
 */
export async function availablePart(api: APIRequestContext, offset = 0): Promise<StockPick> {
  const page = await getJson<StorePage>(api, "/api/v1/store/items?pageSize=48");
  const parts = page.items.filter((item) => item.condition === "New" && item.available > 0);
  const part = parts[offset % Math.max(parts.length, 1)];
  if (!part) throw new Error("The demo store has no new part in stock; reset the demo data.");
  return { code: part.code, modelName: part.modelName };
}

interface PlacedOrder {
  reference: string;
}

/** Places a pickup order for one part the way the storefront does (the signed-in context of `api` is the customer). */
export function placePickupOrder(api: APIRequestContext, part: StockPick): Promise<PlacedOrder> {
  return postJson<PlacedOrder>(api, "/api/v1/orders", {
    customer: { name: uniqueName("عميل اختبار"), phone: uniquePhone(), phoneHasWhatsApp: true, email: null },
    fulfillmentMethod: "Pickup",
    address: null,
    paymentMethod: "CashOnDelivery",
    items: [{ code: part.code, stockItemId: null, assemblyId: null, salePrice: null, parts: null, quantity: 1, priceOption: "Alone" }],
    notes: null,
    usage: null,
    usageNote: null,
    leadSource: null,
    termsAccepted: true
  });
}
