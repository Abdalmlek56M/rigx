"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useCart } from "./cart-context";

const TOAST_MS = 4000;

/** "Added to cart" confirmation shown by `add()`; the live region stays mounted so screen readers announce it. */
export function CartToast() {
  const t = useTranslations("cart");
  const { addedModel, dismissToast, open } = useCart();

  useEffect(() => {
    if (!addedModel) return;
    const timer = window.setTimeout(dismissToast, TOAST_MS);
    return () => window.clearTimeout(timer);
  }, [addedModel, dismissToast]);

  return (
    <div role="status" className="pointer-events-none fixed start-4 top-4 z-40 md:start-8 md:top-6">
      {addedModel ? (
        <div className="toast-in pointer-events-auto flex items-center gap-3 rounded-card bg-carbon px-4 py-3 text-sm text-on-carbon shadow-[0_10px_30px_rgba(0,0,0,.2)]">
          <span aria-hidden="true" className="text-success">✓</span>
          <span>
            {t("added")} <span dir="ltr" className="font-bold [unicode-bidi:isolate]">{addedModel}</span>
          </span>
          <button type="button" onClick={open} className="min-h-11 font-bold text-orange">
            {t("openCart")}
          </button>
        </div>
      ) : null}
    </div>
  );
}
