"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect } from "react";
import { Icon } from "@/components/icons";
import { CartLine } from "@/components/cart-line";
import { useCart } from "@/components/cart-provider";
import { SideDrawer } from "@/components/ui";
import { useLocale } from "@/lib/i18n";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";

export function CartDrawer() {
  const { messages, locale } = useLocale();
  const { items, count, subtotal, notice, noticeTone, isOpen, closeCart } = useCart();
  const pathname = usePathname();

  useEffect(() => {
    closeCart();
  }, [closeCart, pathname]);

  const title = count > 0 ? `${messages.cartTitle} (${count})` : messages.cartTitle;

  return (
    <SideDrawer open={isOpen} title={title} closeLabel={messages.cartClose} onClose={closeCart}>
      <div className="rigx-cart-drawer">
        <p className="rigx-demo-note">
          <Icon name="sparkles" className="h-4 w-4" />
          <span>{messages.cartDemoNotice}</span>
        </p>

        {notice ? (
          <p className={`rigx-cart-notice${noticeTone === "error" ? " is-error" : ""}`} role="status">
            <Icon name={noticeTone === "error" ? "close" : "check"} className="h-4 w-4" />
            <span>{notice}</span>
          </p>
        ) : null}

        {items.length === 0 ? (
          <div className="rigx-cart-empty">
            <span className="rigx-cart-empty-icon">
              <Icon name="shoppingBag" />
            </span>
            <h3>{messages.cartEmptyTitle}</h3>
            <p>{messages.cartEmptyBody}</p>
            <Link href="/products" onClick={closeCart} className="rigx-button rigx-button-primary">
              {messages.cartBrowse}
            </Link>
          </div>
        ) : (
          <>
            <ul className="rigx-cart-lines">
              {items.map((item) => (
                <CartLine key={`${item.productId}-${item.variantId ?? "base"}`} item={item} />
              ))}
            </ul>

            <div className="rigx-cart-summary rigx-cart-summary-drawer">
              <div className="rigx-cart-summary-row">
                <span>{messages.cartSubtotal}</span>
                <strong>
                  {formatCatalogPrice(subtotal)} <small>{currencyLabel(locale)}</small>
                </strong>
              </div>
              <p className="rigx-cart-summary-note">{messages.cartPriceNote}</p>
              <Link href="/checkout" onClick={closeCart} className="rigx-button rigx-button-primary w-full">
                {messages.cartCheckout}
              </Link>
              <div className="rigx-cart-drawer-links">
                <Link href="/cart" onClick={closeCart} className="rigx-cart-text-link">
                  {messages.cartViewCart}
                </Link>
                <button type="button" onClick={closeCart} className="rigx-cart-text-link">
                  {messages.cartContinue}
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </SideDrawer>
  );
}
