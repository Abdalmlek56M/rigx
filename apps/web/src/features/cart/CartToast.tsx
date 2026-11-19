"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { CheckIcon } from "@/components/ui/icons";
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
    <div role="status" className="pointer-events-none fixed end-4 top-[calc(env(safe-area-inset-top)+7.5rem)] z-50 md:end-8 md:top-[5.5rem]">
      {addedModel ? (
        <div className="toast-in pointer-events-auto flex items-center gap-3 rounded-card bg-chrome px-4 py-3 text-sm text-on-carbon shadow-lift">
          <span aria-hidden="true" className="flex h-6 w-6 items-center justify-center rounded-full bg-success text-white"><CheckIcon size={14} /></span>
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
