"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getPublicOffers, type PublicOffer } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { ProductImage } from "@/components/product-image";
import { LoadingCard, Message, SecondaryButton } from "@/components/ui";

function useOffers() {
  const { messages } = useLocale();
  const [offers, setOffers] = useState<PublicOffer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    void getPublicOffers()
      .then((items) => { if (active) setOffers(items); })
      .catch((requestError) => { if (active) setError(apiErrorMessage(requestError, messages)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [attempt, messages]);

  return { offers, loading, error, retry: () => setAttempt((value) => value + 1) };
}

function OfferItemCard({ item }: { item: PublicOffer["items"][number] }) {
  const { messages, locale } = useLocale();
  return (
    <Link
      href={`/products/${encodeURIComponent(item.productSlug)}`}
      className="rigx-offer-card group flex w-[240px] shrink-0 flex-col gap-2 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-3 transition hover:border-[var(--ds-accent)]"
    >
      <div className="rigx-product-image">
        <ProductImage product={{ slug: item.productSlug, sku: item.productSku, categorySlug: item.categorySlug, name: item.productName }} alt={item.productName} />
      </div>
      <p className="line-clamp-2 text-sm font-bold text-[var(--ds-text)]">{item.productName}</p>
      <p className="text-lg font-black text-[var(--ds-accent-strong)]">
        {formatCatalogPrice(item.offerPrice)} <small className="text-xs font-semibold">{currencyLabel(locale)}</small>
      </p>
      <p className="text-xs text-[var(--ds-text-muted)] line-through" dir="ltr">
        {formatCatalogPrice(item.basePrice)} {currencyLabel(locale)}
      </p>
      <span className="text-xs font-semibold text-[var(--ds-accent-strong)]">{messages.wishlistViewProduct}</span>
    </Link>
  );
}

export function OffersExperience() {
  const { messages, locale } = useLocale();
  const { offers, loading, error, retry } = useOffers();

  return (
    <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8">
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX / OFFERS</span>
          <h1>{messages.offersTitle}</h1>
          <p>{messages.offersLead}</p>
        </div>
        <div className="hero-side-stats">
          <span><b>{String(offers.length).padStart(2, "0")}</b><small>{locale === "ar" ? "عرض منشور" : "published offers"}</small></span>
        </div>
      </div>
      {loading ? <LoadingCard /> : error ? (
        <div className="grid gap-3"><Message tone="error">{error}</Message><SecondaryButton type="button" onClick={retry}>{messages.retry}</SecondaryButton></div>
      ) : offers.length === 0 ? (
        <Message tone="info">{messages.offersEmpty}</Message>
      ) : (
        <div className="grid gap-8">
          {offers.map((offer) => (
            <section key={offer.id} aria-label={locale === "ar" ? offer.titleAr : offer.titleEn} className="section-block" style={{ marginTop: 28 }}>
              <div className="section-heading">
                <div>
                  <span className="eyebrow">RIGX / PROMOTION</span>
                  <h2>{locale === "ar" ? offer.titleAr : offer.titleEn}</h2>
                  {offer.descriptionAr || offer.descriptionEn ? (
                    <p>{locale === "ar" ? offer.descriptionAr : offer.descriptionEn}</p>
                  ) : null}
                </div>
                {offer.endsAtUtc ? (
                  <p className="text-xs font-semibold text-[var(--ds-text-muted)]" dir="ltr">
                    {messages.offersEndsAt}: {new Date(offer.endsAtUtc).toLocaleDateString(locale === "ar" ? "ar-EG" : "en-GB")}
                  </p>
                ) : null}
              </div>
              <div className="flex gap-3 overflow-x-auto pb-2">
                {offer.items.map((item) => <OfferItemCard key={item.productId} item={item} />)}
              </div>
            </section>
          ))}
        </div>
      )}
    </main>
  );
}

export function HomeOffers() {
  const { messages, locale } = useLocale();
  const { offers, loading, error } = useOffers();
  if (loading || error || offers.length === 0) return null;
  const first = offers[0];
  return (
    <section className="rigx-section" aria-labelledby="rigx-home-offers-title">
      <div className="rigx-section-heading">
        <div className="rigx-section-heading-copy">
          <p className="rigx-section-kicker">RIGX / OFFERS</p>
          <h2 id="rigx-home-offers-title">{messages.offersTitle}</h2>
          <p>{messages.offersLead}</p>
        </div>
        <Link href="/offers" className="text-sm font-bold text-[var(--ds-accent-strong)]">
          {messages.viewAllProducts}
        </Link>
      </div>
      <p className="mb-3 text-lg font-bold text-[var(--ds-text)]">{locale === "ar" ? first.titleAr : first.titleEn}</p>
      <div className="flex gap-3 overflow-x-auto pb-3">
        {first.items.map((item) => <OfferItemCard key={item.productId} item={item} />)}
      </div>
    </section>
  );
}
