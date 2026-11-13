"use client";

import Link from "next/link";
import { type FormEvent, type InputHTMLAttributes, type TextareaHTMLAttributes, useId, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, createStoreOrder, type StoreOrder } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useCart } from "@/components/cart-provider";
import { Icon } from "@/components/icons";
import { LoadingCard } from "@/components/ui";
import { useLocale } from "@/lib/i18n";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";

type CheckoutFormState = {
  fullName: string;
  phone: string;
  city: string;
  address: string;
  notes: string;
};
type Fulfillment = "Delivery" | "Pickup";
type DeliveryPayment = "CashOnDelivery" | "BankTransfer" | "MobileWalletTransfer";

const emptyForm: CheckoutFormState = { fullName: "", phone: "", city: "", address: "", notes: "" };

function CheckoutField({ label, error, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string }) {
  const generatedId = useId();
  const fieldId = props.id ?? generatedId;
  return (
    <div className="rigx-field">
      <label htmlFor={fieldId}>{label}</label>
      <input {...props} id={fieldId} aria-invalid={Boolean(error)} aria-describedby={error ? `${fieldId}-error` : undefined} className="rigx-input" />
      {error ? <p id={`${fieldId}-error`} className="rigx-field-error">{error}</p> : null}
    </div>
  );
}

function CheckoutTextarea({ label, error, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; error?: string }) {
  const generatedId = useId();
  const fieldId = props.id ?? generatedId;
  return (
    <div className="rigx-field">
      <label htmlFor={fieldId}>{label}</label>
      <textarea {...props} id={fieldId} aria-invalid={Boolean(error)} aria-describedby={error ? `${fieldId}-error` : undefined} className="rigx-input rigx-textarea" />
      {error ? <p id={`${fieldId}-error`} className="rigx-field-error">{error}</p> : null}
    </div>
  );
}

export function CheckoutExperience() {
  const { messages, locale } = useLocale();
  const isArabicCheckout = locale === "ar";
  const router = useRouter();
  const { items, ready, subtotal, clear } = useCart();
  const [form, setForm] = useState<CheckoutFormState>(emptyForm);
  const [errors, setErrors] = useState<Partial<Record<keyof CheckoutFormState | "terms", string>>>({});
  const [fulfillment, setFulfillment] = useState<Fulfillment>("Delivery");
  const [payment, setPayment] = useState<DeliveryPayment>("CashOnDelivery");
  const [termsAccepted, setTermsAccepted] = useState(false);
  const [order, setOrder] = useState<StoreOrder | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function updateField(field: keyof CheckoutFormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors: Partial<Record<keyof CheckoutFormState | "terms", string>> = {};
    if (!form.fullName.trim()) nextErrors.fullName = messages.required;
    if (!form.phone.trim()) nextErrors.phone = messages.required;
    if (fulfillment === "Delivery") {
      if (!form.city.trim()) nextErrors.city = messages.required;
      if (!form.address.trim()) nextErrors.address = messages.required;
    }
    if (!termsAccepted) nextErrors.terms = messages.checkoutTermsRequired;
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0 || items.length === 0) return;
    setSubmitting(true);
    setError(null);
    try {
      const created = await createStoreOrder({
        customerName: form.fullName.trim(),
        phoneNumber: form.phone.trim(),
        addressText: fulfillment === "Delivery" ? `${form.city.trim()} - ${form.address.trim()}` : null,
        fulfillmentMethod: fulfillment,
        paymentMethod: fulfillment === "Delivery" ? payment : "ToBeArranged",
        notes: form.notes.trim() || null,
        termsAccepted: true,
        items: items.map((item) => ({
          productId: item.assemblyId ? null : item.productId,
          assemblyId: item.assemblyId,
          variantId: item.variantId,
          name: item.name,
          sku: item.sku,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
        })),
      });
      setOrder(created);
      clear();
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 401) {
        router.push(`/login?returnTo=${encodeURIComponent("/checkout")}`);
        return;
      }
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSubmitting(false);
    }
  }

  if (!ready) return <main className="rigx-checkout-page"><LoadingCard /></main>;

  if (order) {
    return (
      <main className="rigx-checkout-page">
        <div className="rigx-checkout-success">
          <span className="rigx-checkout-success-icon"><Icon name="check" /></span>
          <p className="rigx-section-kicker">RIGX / ORDER</p>
          <h1>{messages.checkoutSuccessTitle}</h1>
          <p className="rigx-checkout-success-lead">
            {order.fulfillmentMethod === "Delivery" ? messages.checkoutSuccessDelivery : messages.checkoutSuccessPickup}
          </p>
          <div className="rigx-demo-reference"><span>{messages.checkoutOrderRef}</span><strong dir="ltr">{order.reference}</strong></div>
          <p className="mt-2 text-sm font-semibold text-[var(--ds-text-muted)]">
            {messages.checkoutStatus}: {order.status} · {messages.checkoutPayment}: {order.paymentStatus}
          </p>
          <div className="rigx-order-summary">
            <h2>{messages.checkoutOrderSummary}</h2>
            <ul>
              {order.items.map((item) => (
                <li key={item.id}>
                  <span>{item.name}{item.variantName ? ` · ${item.variantName}` : ""}<small> × {item.quantity}</small></span>
                  <strong>{formatCatalogPrice(item.lineTotal)}</strong>
                </li>
              ))}
            </ul>
            <div className="rigx-cart-summary-row">
              <span>{messages.cartSubtotal}</span>
              <strong>{formatCatalogPrice(order.subtotal)} <small>{currencyLabel(locale)}</small></strong>
            </div>
            <div className="rigx-cart-summary-row">
              <span>{messages.checkoutDeliveryFee}</span>
              <strong>{order.deliveryFee > 0 ? formatCatalogPrice(order.deliveryFee) : messages.checkoutDeliveryFeePending}</strong>
            </div>
          </div>
          <p className="rigx-demo-note"><Icon name="sparkles" className="h-4 w-4" /><span>{messages.checkoutNoPayment}</span></p>
          <div className="rigx-checkout-success-actions">
            <Link href="/account/orders" className="rigx-button rigx-button-primary">{messages.accountOrdersSection}</Link>
            <Link href="/products" className="rigx-cart-secondary-button">{messages.checkoutBackToStore}</Link>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="rigx-checkout-page">
      <div className="page-hero compact-hero">
        <div>
          <p className="eyebrow">RIGX / CHECKOUT</p>
          <h1>{messages.checkoutTitle}</h1>
          <p>{messages.checkoutLead}</p>
        </div>
        <div className="cart-stepper">
          <span>01 {isArabicCheckout ? "السلة" : "Cart"}</span><i />
          <span className="step-active">02 {isArabicCheckout ? "الطلب" : "Checkout"}</span><i />
          <span>03 {isArabicCheckout ? "التأكيد" : "Confirm"}</span>
        </div>
      </div>

      {items.length === 0 ? (
        <div className="rigx-cart-empty rigx-cart-empty-page">
          <span className="rigx-cart-empty-icon"><Icon name="shoppingBag" /></span>
          <h2>{messages.checkoutEmptyTitle}</h2>
          <p>{messages.checkoutEmptyBody}</p>
          <Link href="/products" className="rigx-button rigx-button-primary">{messages.cartBrowse}</Link>
        </div>
      ) : (
        <div className="rigx-checkout-layout">
          <form className="rigx-checkout-form" onSubmit={handleSubmit} noValidate>
            <section aria-labelledby="checkout-contact-heading">
              <h2 id="checkout-contact-heading">{messages.checkoutContactSection}</h2>
              <div className="rigx-checkout-fields">
                <CheckoutField label={messages.fullName} value={form.fullName} onChange={(event) => updateField("fullName", event.target.value)} autoComplete="name" error={errors.fullName} />
                <CheckoutField label={messages.phoneNumber} value={form.phone} onChange={(event) => updateField("phone", event.target.value)} type="tel" inputMode="tel" dir="ltr" autoComplete="tel" error={errors.phone} />
              </div>
            </section>

            <section aria-labelledby="checkout-fulfillment-heading">
              <h2 id="checkout-fulfillment-heading">{messages.checkoutFulfillment}</h2>
              <div className="rigx-checkout-choice-grid">
                {(["Delivery", "Pickup"] as const).map((method) => (
                  <button
                    key={method}
                    type="button"
                    onClick={() => setFulfillment(method)}
                    className={`rigx-checkout-choice${fulfillment === method ? " is-selected" : ""}`}
                    aria-pressed={fulfillment === method}
                  >
                    <strong>{method === "Delivery" ? messages.checkoutDelivery : messages.checkoutPickup}</strong>
                    <span>{method === "Delivery" ? messages.checkoutDeliveryHint : messages.checkoutPickupHint}</span>
                  </button>
                ))}
              </div>
            </section>

            {fulfillment === "Delivery" ? (
              <section aria-labelledby="checkout-delivery-heading">
                <h2 id="checkout-delivery-heading">{messages.checkoutDeliverySection}</h2>
                <div className="rigx-checkout-fields">
                  <CheckoutField label={messages.checkoutCity} value={form.city} onChange={(event) => updateField("city", event.target.value)} autoComplete="address-level2" error={errors.city} />
                  <CheckoutField label={messages.checkoutAddress} value={form.address} onChange={(event) => updateField("address", event.target.value)} autoComplete="street-address" error={errors.address} />
                </div>
                <p className="rigx-cart-summary-note">{messages.checkoutDeliveryFeePending}</p>
              </section>
            ) : (
              <section className="rigx-checkout-pickup-note" aria-labelledby="checkout-pickup-heading">
                <h2 id="checkout-pickup-heading">{messages.checkoutPickup}</h2>
                <p>{messages.checkoutPickupNote}</p>
              </section>
            )}

            {fulfillment === "Delivery" ? (
              <section aria-labelledby="checkout-payment-heading">
                <h2 id="checkout-payment-heading">{messages.checkoutPaymentMethod}</h2>
                <div className="rigx-checkout-choice-grid">
                  {[
                    ["CashOnDelivery", messages.checkoutCod],
                    ["BankTransfer", messages.checkoutBank],
                    ["MobileWalletTransfer", messages.checkoutWallet],
                  ].map(([value, label]) => (
                    <button key={value} type="button" onClick={() => setPayment(value as DeliveryPayment)}
                      className={`rigx-checkout-choice${payment === value ? " is-selected" : ""}`}
                      aria-pressed={payment === value}>
                      <strong>{label}</strong>
                    </button>
                  ))}
                </div>
              </section>
            ) : null}

            <section aria-labelledby="checkout-notes-heading">
              <h2 id="checkout-notes-heading">{messages.checkoutNotesSection}</h2>
              <CheckoutTextarea label={messages.checkoutNotes} value={form.notes} onChange={(event) => updateField("notes", event.target.value)} rows={3} />
            </section>

            <label className="rigx-checkout-terms">
              <input type="checkbox" checked={termsAccepted} onChange={(event) => { setTermsAccepted(event.target.checked); setErrors((current) => ({ ...current, terms: undefined })); }} />
              <span>{messages.checkoutTerms}</span>
            </label>
            {errors.terms ? <p className="rigx-field-error">{errors.terms}</p> : null}
            <div className="flex flex-wrap gap-3 text-sm">
              <Link href="/policies/terms" className="font-bold text-[var(--ds-accent)]">{messages.checkoutTermsLink}</Link>
              <Link href="/policies/privacy" className="font-bold text-[var(--ds-accent)]">{messages.checkoutPrivacyLink}</Link>
            </div>

            {error ? <p role="alert" className="rigx-field-error">{error}</p> : null}
            <button type="submit" className="rigx-button rigx-button-primary w-full" disabled={submitting}>
              {submitting ? messages.loading : messages.checkoutSubmit}
            </button>
            <Link href="/cart" className="rigx-cart-secondary-button">{messages.checkoutBackToCart}</Link>
          </form>

          <aside className="rigx-cart-summary rigx-checkout-summary" aria-labelledby="checkout-summary-heading">
            <h2 id="checkout-summary-heading">{messages.cartSummaryTitle}</h2>
            <ul className="rigx-checkout-mini-lines">
              {items.map((item) => (
                <li key={`${item.productId}-${item.variantId ?? "base"}`}>
                  <span>{item.name}{item.variantName ? ` · ${item.variantName}` : ""}<small> × {item.quantity}</small></span>
                  <strong>{formatCatalogPrice(Math.round(item.unitPrice * item.quantity * 100) / 100)}</strong>
                </li>
              ))}
            </ul>
            <div className="rigx-cart-summary-row"><span>{messages.cartSubtotal}</span><strong>{formatCatalogPrice(subtotal)} <small>{currencyLabel(locale)}</small></strong></div>
            <div className="rigx-cart-summary-row"><span>{messages.checkoutDeliveryFee}</span><strong>{messages.checkoutDeliveryFeePending}</strong></div>
            <p className="rigx-cart-summary-note">{messages.cartPriceNote}</p>
            <p className="rigx-checkout-payment-note">{fulfillment === "Delivery" ? messages.checkoutNoPayment : messages.checkoutPickupNoPayment}</p>
          </aside>
        </div>
      )}
    </main>
  );
}
