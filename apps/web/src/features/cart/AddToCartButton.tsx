"use client";

import type { ComponentType, ReactNode } from "react";
import { useTranslations } from "next-intl";
import { Button, type ButtonSize, type ButtonVariant } from "@/components/ui/Button";
import { CartIcon } from "@/components/ui/icons";
import { useCart } from "./cart-context";
import type { CartLine } from "./cart-rules";

interface AddToCartButtonProps {
  line: CartLine;
  /** Also opens the cart drawer ("تمت الإضافة"). */
  thenOpenCart?: boolean;
  variant?: ButtonVariant;
  size?: ButtonSize;
  /** Defaults to the cart icon; pass `null` for a text-only button. */
  icon?: ComponentType<{ size?: number }> | null;
  className?: string;
  children?: ReactNode;
}

/** Adds one line to the cart (the toast confirms it); the label defaults to "أضف للسلة". */
export function AddToCartButton({ line, thenOpenCart = false, variant = "accent", size = "md", icon = CartIcon, className, children }: AddToCartButtonProps) {
  const t = useTranslations("cart");
  const { add, open } = useCart();

  return (
    <Button
      type="button"
      variant={variant}
      size={size}
      icon={icon ?? undefined}
      className={className}
      onClick={() => {
        add(line);
        if (thenOpenCart) open();
      }}
    >
      {children ?? t("add")}
    </Button>
  );
}
