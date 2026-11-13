"use client";

import Link from "next/link";
import { useState } from "react";
import { useCart } from "@/components/cart-provider";
import { useLocale } from "@/lib/i18n";
import { ProductImage } from "@/components/product-image";
import { LoadingCard } from "@/components/ui";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { cartLineKey } from "@/lib/cart";

export function RigxCart() {
  const { locale, messages } = useLocale();
  const isArabic = locale === "ar";
  const { items, ready, count, subtotal, setQuantity, removeItem, clear } = useCart();
  const [confirmClear, setConfirmClear] = useState(false);

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">SHOPPING / CART</span>
          <h1>{isArabic ? <>سلة <em>التسوق.</em></> : <>Your <em>cart.</em></>}</h1>
          <p>{isArabic ? "راجع المكوّنات والكميات قبل إتمام الطلب." : "Review components and quantities before checkout."}</p>
        </div>
        <div className="cart-stepper">
          <span className="step-active">01 {isArabic ? "السلة" : "Cart"}</span><i />
          <span>02 {isArabic ? "الطلب" : "Checkout"}</span><i />
          <span>03 {isArabic ? "التأكيد" : "Confirm"}</span>
        </div>
      </div>

      {!ready ? <LoadingCard /> : (
        <div className="cart-layout">
          <section className="cart-items glass-panel">
            {items.length === 0 ? (
              <div className="cart-empty">
                <div className="empty-icon">+</div>
                <h3>{messages.cartEmptyTitle}</h3>
                <p>{messages.cartEmptyBody}</p>
                <Link className="btn-primary" href="/products">{isArabic ? "استعرض المنتجات" : "Browse products"} ‹</Link>
              </div>
            ) : (
              <>
                <div className="panel-title">
                  <div>
                    <span className="eyebrow">CART ITEMS</span>
                    <h2>{isArabic ? `عناصر السلة (${count})` : `Cart items (${count})`}</h2>
                  </div>
                  <button type="button" className="text-link" onClick={() => setConfirmClear(true)}>{messages.cartClear}</button>
                </div>
                <ul className="cart-line-list">
                {items.map((item) => {
                  const key = cartLineKey(item);
                  return (
                    <li className="cart-line rigx-cart-line" key={key}>
                      <Link href={item.assemblyId ? "/builds" : `/products/${encodeURIComponent(item.slug)}`}>
                        <ProductImage
                          alt={item.name}
                          product={{ slug: item.slug, sku: item.sku, categorySlug: item.categorySlug, name: item.name }}
                        />
                      </Link>
                      <div className="cart-line-info">
                        <h4>
                          <Link href={item.assemblyId ? "/builds" : `/products/${encodeURIComponent(item.slug)}`}>
                            {item.name}{item.variantName ? ` — ${item.variantName}` : ""}
                          </Link>
                        </h4>
                        <p dir="ltr">{item.sku} · {item.categorySlug}</p>
                        <div className="cart-qty-controls">
                          <button type="button" onClick={() => setQuantity(key, item.quantity - 1)} aria-label={isArabic ? "تقليل الكمية" : "Decrease quantity"}>−</button>
                          <b>× {item.quantity}</b>
                          <button type="button" onClick={() => setQuantity(key, item.quantity + 1)} aria-label={isArabic ? "زيادة الكمية" : "Increase quantity"}>+</button>
                        </div>
                        <button type="button" className="remove-line" onClick={() => removeItem(key)}>{isArabic ? "إزالة" : "Remove"}</button>
                      </div>
                      <span className="cart-line-total rigx-cart-line-price" dir="ltr"><strong>{formatCatalogPrice(item.unitPrice * item.quantity)} {currencyLabel(locale)}</strong></span>
                    </li>
                  );
                })}
                </ul>
              </>
            )}
          </section>

          <aside className="cart-summary glass-panel rigx-cart-summary">
            <span className="eyebrow">ORDER SUMMARY</span>
            <h3>{isArabic ? "ملخص الطلب" : "Order summary"}</h3>
            <div className="summary-line rigx-cart-summary-row"><span>{isArabic ? "المجموع الفرعي" : "Subtotal"}</span><strong dir="ltr">{formatCatalogPrice(subtotal)} {currencyLabel(locale)}</strong></div>
            <div className="summary-line rigx-cart-summary-row"><span>{isArabic ? "رسوم التوصيل" : "Delivery fee"}</span><b>{isArabic ? "يحددها RIGX" : "Confirmed by RIGX"}</b></div>
            <div className="summary-total"><span>{isArabic ? "الإجمالي" : "Total"}</span><b dir="ltr">{formatCatalogPrice(subtotal)} {currencyLabel(locale)}</b></div>
            <p className="summary-disclaimer">
              {isArabic
                ? "الإجمالي لا يشمل رسوم التوصيل. لا يوجد دفع إلكتروني؛ طريقة الدفع تُسجل مع الطلب ويؤكدها فريق RIGX."
                : "The total excludes the delivery fee. There is no online payment; the payment method is recorded with the order and confirmed by RIGX."}
            </p>
            <Link className={`btn-primary full-width${items.length === 0 ? " is-disabled" : ""}`} href={items.length ? "/checkout" : "/products"} aria-disabled={items.length === 0}>
              {isArabic ? "متابعة إلى إتمام الطلب" : "Continue to checkout"} ‹
            </Link>
            <Link className="btn-outline full-width" href="/products">{isArabic ? "متابعة التسوق" : "Continue shopping"}</Link>
          </aside>
        </div>
      )}

      {confirmClear ? (
        <div className="modal-backdrop" role="alertdialog" aria-modal="true" aria-label={messages.cartClear}>
          <div className="preview-modal glass-panel">
            <h2 className="modal-title">{messages.cartClear}</h2>
            <p className="inline-note">{messages.cartClearConfirm}</p>
            <div className="product-page-actions">
              <button type="button" className="btn-primary" onClick={() => { clear(); setConfirmClear(false); }}>{messages.cartClear}</button>
              <button type="button" className="btn-outline" onClick={() => setConfirmClear(false)}>{isArabic ? "إلغاء" : "Cancel"}</button>
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}

