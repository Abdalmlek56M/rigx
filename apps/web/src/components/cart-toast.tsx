"use client";

import { Icon } from "@/components/icons";
import { useCart } from "@/components/cart-provider";
import { useLocale } from "@/lib/i18n";

export function CartToast() {
  const { toast, dismissToast, openCart } = useCart();
  const { messages } = useLocale();

  if (!toast) return null;

  return (
    <div className="rigx-cart-toast" role="status" aria-live="polite">
      <span className="rigx-cart-toast-icon" aria-hidden="true">
        <Icon name="check" />
      </span>
      <div className="rigx-cart-toast-copy">
        <strong>{toast.message}</strong>
        {toast.productName ? (
          <span dir="auto">
            {toast.productName}
            {toast.variantName ? <span className="rigx-cart-toast-variant">{toast.variantName}</span> : null}
          </span>
        ) : null}
      </div>
      <button
        type="button"
        className="rigx-cart-toast-action"
        onClick={() => {
          dismissToast();
          openCart();
        }}
      >
        {messages.cartViewCart}
      </button>
      <button
        type="button"
        className="rigx-cart-toast-close"
        aria-label={messages.dismissNotification}
        title={messages.dismissNotification}
        onClick={dismissToast}
      >
        <Icon name="close" />
      </button>
    </div>
  );
}
