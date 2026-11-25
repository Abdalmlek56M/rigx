"use client";

import { useTranslations } from "next-intl";
import { CartIcon } from "@/components/ui/icons";
import { CountBadge } from "@/components/ui/CountBadge";
import { HEADER_ICON_BUTTON } from "@/components/layout/nav-links";
import { useCart } from "./cart-context";

/** Header cart control: an outlined pill with the cart icon, an orange count badge and, from xl, the word "السلة"; the count is in the accessible name. */
export function CartButton() {
  const t = useTranslations("cart");
  const { count, open } = useCart();

  return (
    <button type="button" aria-label={t("buttonAria", { count })} aria-haspopup="dialog" onClick={open} className={`relative ${HEADER_ICON_BUTTON} border border-line bg-paper xl:ps-3 xl:pe-4`}>
      <CartIcon />
      <span className="hidden text-sm font-bold xl:inline">{t("title")}</span>
      {count > 0 ? <CountBadge key={count}>{count > 99 ? "99+" : count}</CountBadge> : null}
    </button>
  );
}
