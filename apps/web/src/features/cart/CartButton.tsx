"use client";

import { useTranslations } from "next-intl";
import { CartIcon } from "@/components/ui/icons";
import { useCart } from "./cart-context";

/** Header cart control: icon on mobile, orange labelled button with the count badge from md up. */
export function CartButton() {
  const t = useTranslations("cart");
  const { count, open } = useCart();

  return (
    <button
      type="button"
      aria-label={t("buttonAria", { count })}
      aria-haspopup="dialog"
      onClick={open}
      className="flex h-11 min-w-11 items-center justify-center gap-2 rounded-control px-2.5 font-bold text-on-carbon md:bg-orange md:text-on-orange"
    >
      <CartIcon size={20} />
      <span className="hidden md:inline">{t("button")}</span>
      {count > 0 ? (
        <span aria-hidden="true" className="rounded-full bg-orange px-[7px] font-mono text-xs text-on-orange md:bg-carbon md:text-on-carbon">
          {count}
        </span>
      ) : null}
    </button>
  );
}
