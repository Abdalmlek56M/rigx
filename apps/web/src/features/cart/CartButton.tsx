"use client";

import { useTranslations } from "next-intl";
import { CartIcon } from "@/components/ui/icons";
import { CountBadge } from "@/components/ui/CountBadge";
import { HEADER_ICON_BUTTON } from "@/components/layout/nav-links";
import { useCart } from "./cart-context";

/** Header cart control: the cart icon alone with an orange count badge; the count is in the accessible name. */
export function CartButton() {
  const t = useTranslations("cart");
  const { count, open } = useCart();

  return (
    <button type="button" aria-label={t("buttonAria", { count })} aria-haspopup="dialog" onClick={open} className={`relative ${HEADER_ICON_BUTTON}`}>
      <CartIcon />
      {count > 0 ? <CountBadge key={count}>{count > 99 ? "99+" : count}</CountBadge> : null}
    </button>
  );
}
