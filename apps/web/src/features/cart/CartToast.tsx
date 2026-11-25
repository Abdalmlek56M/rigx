"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { CheckIcon } from "@/components/ui/icons";
import { TOAST_CARD_CLASS, TOAST_REGION_CLASS, TOAST_TONE_CLASS } from "@/components/ui/toast-class";
import { useCart } from "./cart-context";

const TOAST_MS = 4000;

/** "Added to cart" confirmation shown by `add()` at the top of the screen; the live region stays mounted so screen readers announce it. */
export function CartToast() {
  const t = useTranslations("cart");
  const { addedModel, dismissToast, open } = useCart();
  const tone = TOAST_TONE_CLASS.success;

  useEffect(() => {
    if (!addedModel) return;
    const timer = window.setTimeout(dismissToast, TOAST_MS);
    return () => window.clearTimeout(timer);
  }, [addedModel, dismissToast]);

  return (
    <div role="status" className={TOAST_REGION_CLASS}>
      {addedModel ? (
        <div className={`toast-in ${TOAST_CARD_CLASS} items-center ${tone.edge}`}>
          <span aria-hidden="true" className={`flex size-8 shrink-0 items-center justify-center rounded-full ${tone.icon}`}><CheckIcon size={16} /></span>
          <span className="min-w-0 flex-1 text-sm leading-6">
            {t("added")} <span dir="ltr" className="font-bold [unicode-bidi:isolate]">{addedModel}</span>
          </span>
          <button type="button" onClick={open} className="min-h-11 shrink-0 rounded-control px-2 text-sm font-bold text-orange-text transition-colors hover:bg-orange-soft">
            {t("openCart")}
          </button>
        </div>
      ) : null}
    </div>
  );
}
