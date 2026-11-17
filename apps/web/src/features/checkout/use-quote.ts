"use client";

import { useEffect, useState } from "react";
import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";

export type Quote = Schemas["CheckoutQuoteView"];

const DEBOUNCE_MS = 250;

/** Shipping and upfront amounts for the chosen place; `null` until they are known (delivery needs a governorate first). */
export function useQuote(pickup: boolean, governorate: string, subtotal: number): Quote | null {
  const key = pickup || governorate ? `${pickup ? 1 : 0}|${governorate}|${subtotal}` : "";
  const settled = useDebouncedValue(key, DEBOUNCE_MS);
  const [result, setResult] = useState<{ key: string; quote: Quote } | null>(null);

  useEffect(() => {
    if (!settled) return;
    const [pickupFlag, place, amount] = settled.split("|");
    let cancelled = false;
    void required(
      unwrap(browserApi().GET("/api/v1/storefront/checkout-quote", {
        params: { query: { governorate: place ?? "", subtotal: Number(amount), pickup: pickupFlag === "1" } }
      }))
    )
      .then((quote) => {
        if (!cancelled) setResult({ key: settled, quote });
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [settled]);

  return result && result.key === key ? result.quote : null;
}
