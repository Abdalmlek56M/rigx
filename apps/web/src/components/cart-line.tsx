"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { ProductImage } from "@/components/product-image";
import { useCart } from "@/components/cart-provider";
import { useLocale } from "@/lib/i18n";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { CART_MAX_QUANTITY, cartLineKey, type CartItem } from "@/lib/cart";

export function CartLine({ item }: { item: CartItem }) {
  const { messages, locale } = useLocale();
  const { setQuantity, removeItem } = useCart();
  const key = cartLineKey(item);
  const lineTotal = Math.round(item.unitPrice * item.quantity * 100) / 100;

  return (
    <li className="rigx-cart-line">
      <Link
        href={item.assemblyId ? "/builds" : `/products/${encodeURIComponent(item.slug)}`}
        className="rigx-cart-line-media"
        aria-label={item.name}
      >
        <ProductImage
          product={{ slug: item.slug, sku: item.sku, categorySlug: item.categorySlug }}
          alt={item.name}
          compact
        />
      </Link>

      <div className="rigx-cart-line-body">
        <div className="rigx-cart-line-head">
          <div>
            <Link href={item.assemblyId ? "/builds" : `/products/${encodeURIComponent(item.slug)}`} className="rigx-cart-line-name">
              {item.name}
            </Link>
            <p className="rigx-cart-line-meta">
              {item.variantName ?? messages.cartBaseVariant}
              <span aria-hidden="true"> · </span>
              <span dir="ltr">{item.sku}</span>
            </p>
          </div>
          <button
            type="button"
            onClick={() => removeItem(key)}
            aria-label={`${messages.cartRemove}: ${item.name}`}
            title={messages.cartRemove}
            className="rigx-cart-icon-button"
          >
            <Icon name="trash" className="h-4 w-4" />
          </button>
        </div>

        <div className="rigx-cart-line-foot">
          <div className="rigx-qty" role="group" aria-label={`${messages.cartQuantity}: ${item.name}`}>
            <button
              type="button"
              onClick={() => setQuantity(key, item.quantity - 1)}
              disabled={item.quantity <= 1}
              aria-label={messages.cartDecrease}
              title={messages.cartDecrease}
            >
              <Icon name="minus" className="h-3.5 w-3.5" />
            </button>
            <span aria-live="polite">{item.quantity}</span>
            <button
              type="button"
              onClick={() => setQuantity(key, item.quantity + 1)}
              disabled={item.quantity >= CART_MAX_QUANTITY}
              aria-label={messages.cartIncrease}
              title={messages.cartIncrease}
            >
              <Icon name="plus" className="h-3.5 w-3.5" />
            </button>
          </div>

          <div className="rigx-cart-line-price">
            <strong>
              {formatCatalogPrice(lineTotal)} <small>{currencyLabel(locale)}</small>
            </strong>
            {item.quantity > 1 ? (
              <span dir="ltr">
                {formatCatalogPrice(item.unitPrice)} × {item.quantity}
              </span>
            ) : null}
          </div>
        </div>
      </div>
    </li>
  );
}
