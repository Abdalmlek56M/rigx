/**
 * Free shipping, as the storefront words it. The owner turns it on in the settings by giving a minimum order total; the server decides
 * it again on the real order (after the coupon, before shipping), so this only decides what the page says.
 */

/** The hint "add X for free shipping" appears once the total has reached this share of the minimum. */
export const NEAR_SHARE = 0.5;

const cents = (value: number) => Math.round(value * 100) / 100;

/** The minimum from the settings, or null when free shipping is off. */
export function freeShippingMinimum(enabled: boolean | undefined, minimum: number | undefined): number | null {
  return enabled && minimum !== undefined && minimum > 0 ? minimum : null;
}

/** What the total still lacks for free shipping; null when it is off or already reached. */
export function freeShippingRemaining(minimum: number | null, total: number): number | null {
  if (minimum === null || total >= minimum) return null;
  return cents(minimum - total);
}

/** The amount to show in "add X for free shipping": only when it is close, never for a nearly empty cart. */
export function freeShippingHint(minimum: number | null, remaining: number | null): number | null {
  if (minimum === null || remaining === null || remaining <= 0) return null;
  return remaining <= minimum * (1 - NEAR_SHARE) ? remaining : null;
}

/** The "free" shipping reached by this total (cart pages; the checkout reads the server's quote instead). */
export function freeShippingReached(minimum: number | null, total: number): boolean {
  return minimum !== null && total >= minimum;
}
