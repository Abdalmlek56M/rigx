import "server-only";
import { unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";

/** Pickup setting and the governorates with their fees; `null` when the API does not answer (the page then says so). */
export async function loadShippingRates(): Promise<Schemas["ShippingRatesView"] | null> {
  try {
    return await required(unwrap(createServerApi().GET("/api/v1/storefront/shipping-rates")));
  } catch (error) {
    console.warn("Shipping rates unavailable:", error instanceof Error ? error.message : error);
    return null;
  }
}
