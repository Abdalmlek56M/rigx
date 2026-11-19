"use client";

import { useTranslations } from "next-intl";
import { CartIcon } from "@/components/ui/icons";
import { HEADER_ICON_BUTTON } from "@/components/layout/nav-links";
import { useCart } from "./cart-context";

/** Header cart control: the cart icon alone with an orange count badge; the count is in the accessible name. */
export function CartButton() {
  const t = useTranslations("cart");
  const { count, open } = useCart();

  return (
    <button type="button" aria-label={t("buttonAria", { count })} aria-haspopup="dialog" onClick={open} className={`relative ${HEADER_ICON_BUTTON}`}>
      <CartIcon />
      {count > 0 ? (
        <span
          key={count}
          aria-hidden="true"
          className="badge-pop absolute -top-0.5 end-0 flex h-5 min-w-5 items-center justify-center rounded-full bg-orange px-1.5 font-mono text-[11px] font-semibold leading-none text-on-orange ring-2 ring-paper"
        >
          {count > 99 ? "99+" : count}
        </span>
      ) : null}
    </button>
  );
}
