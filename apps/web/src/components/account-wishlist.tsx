"use client";

import Link from "next/link";
import { useState } from "react";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { useWishlist } from "@/components/wishlist-provider";
import { ProductImage } from "@/components/product-image";
import { ConfirmDialog, LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton } from "@/components/ui";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";
import { AccountShell } from "@/components/account-shell";

export function AccountWishlist() {
  const { messages, locale } = useLocale();
  const { status, refresh: refreshSession } = useSession();
  const { items, loading, notice, pendingIds, remove, clear, refresh, dismissNotice } = useWishlist();
  const [confirmClear, setConfirmClear] = useState(false);
  const [clearing, setClearing] = useState(false);

  async function confirmClearAll() {
    setClearing(true);
    try {
      await clear();
      setConfirmClear(false);
    } finally {
      setClearing(false);
    }
  }

  return (
    <AccountShell>
      <PageIntro eyebrow={messages.accountEyebrow} title={messages.wishlistTitle} lead={messages.wishlistLead} />

      

      {status === "anonymous" ? (
        <Message tone="info">
          {messages.wishlistGuestNotice}{" "}
          <Link href="/login" className="font-bold underline">{messages.navLogin}</Link>
        </Message>
      ) : null}
      {status === "unavailable" ? (
        <div className="flex flex-wrap items-center gap-3">
          <Message tone="error">{messages.accountUnavailable}</Message>
          <SecondaryButton type="button" onClick={() => void refreshSession()}>{messages.retry}</SecondaryButton>
        </div>
      ) : null}

      {notice ? (
        <div className="flex flex-wrap items-center gap-3">
          <Message tone={notice.tone}>{notice.text}</Message>
          <button type="button" className="text-sm font-semibold text-[var(--ds-accent)]" onClick={dismissNotice}>
            {messages.dismissNotification}
          </button>
        </div>
      ) : null}

      {loading ? (
        <LoadingCard />
      ) : items.length === 0 ? (
        <div className="grid gap-4">
          <Message tone="info">{messages.wishlistEmpty}</Message>
          <Link href="/products" className="rigx-button rigx-button-primary w-fit">
            {messages.wishlistBrowse}
          </Link>
        </div>
      ) : (
        <>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--ds-text-muted)]">
              {messages.wishlistCount.replace("{count}", String(items.length))}
            </p>
            <SecondaryButton type="button" onClick={() => setConfirmClear(true)}>{messages.wishlistClear}</SecondaryButton>
          </div>
          <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {items.map((item) => (
              <article key={item.productId} className="rigx-admin-card flex flex-col gap-3 p-4">
                <Link href={`/products/${encodeURIComponent(item.slug)}`} className="block">
                  <div className="rigx-product-image">
                    <ProductImage
                      product={{ slug: item.slug, sku: item.sku, categorySlug: item.categorySlug, name: item.name }}
                      alt={item.name}
                    />
                  </div>
                </Link>
                <div className="min-w-0">
                  <Link
                    href={`/products/${encodeURIComponent(item.slug)}`}
                    className="line-clamp-2 font-bold text-[var(--ds-text)] hover:text-[var(--ds-accent-strong)]"
                  >
                    {item.name}
                  </Link>
                  <p className="mt-1 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{item.sku}</p>
                  <p className="mt-2 font-bold text-[var(--ds-text)]">
                    {formatCatalogPrice(item.basePrice)} <small className="font-semibold text-[var(--ds-text-muted)]">{currencyLabel(locale)}</small>
                  </p>
                  {!item.isAvailable ? (
                    <p className="mt-1 text-xs font-semibold text-[var(--ds-text-muted)]">{messages.wishlistUnavailableProduct}</p>
                  ) : null}
                </div>
                <div className="mt-auto flex flex-wrap gap-2">
                  <PrimaryButton type="button" pending={pendingIds.has(item.productId)} onClick={() => void remove(item.productId)}>
                    {messages.wishlistRemoveAction}
                  </PrimaryButton>
                  <Link href={`/products/${encodeURIComponent(item.slug)}`} className="rigx-button rigx-button-secondary">
                    {messages.wishlistViewProduct}
                  </Link>
                </div>
              </article>
            ))}
          </section>
        </>
      )}

      <ConfirmDialog
        open={confirmClear}
        title={messages.wishlistClear}
        description={messages.wishlistClearConfirm}
        confirmLabel={messages.wishlistClear}
        cancelLabel={messages.adminCancel}
        pending={clearing}
        onConfirm={() => void confirmClearAll()}
        onCancel={() => setConfirmClear(false)}
      />
    </AccountShell>
  );
}


