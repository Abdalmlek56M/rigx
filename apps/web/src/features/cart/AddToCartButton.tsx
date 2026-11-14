"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { useCart } from "./cart-context";
import type { CartLine } from "./cart-rules";

interface AddToCartButtonProps {
  line: CartLine;
  /** Also opens the cart drawer ("اشتري دلوقتي"). */
  thenOpenCart?: boolean;
  className: string;
  children?: ReactNode;
}

/** Adds one line to the cart (the toast confirms it); the label defaults to "أضف للسلة". */
export function AddToCartButton({ line, thenOpenCart = false, className, children }: AddToCartButtonProps) {
  const t = useTranslations("cart");
  const { add, open } = useCart();

  return (
    <button
      type="button"
      className={className}
      onClick={() => {
        add(line);
        if (thenOpenCart) open();
      }}
    >
      {children ?? t("add")}
    </button>
  );
}
