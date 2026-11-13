"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  deleteStoreReview,
  getCatalogProducts,
  getMyReviews,
  upsertStoreReview,
  type CatalogProductSummary,
  type MyReviews,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { AccountShell } from "@/components/account-shell";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton } from "@/components/ui";

const statusLabel = (status: string, messages: ReturnType<typeof useLocale>["messages"]) =>
  status === "Published" ? messages.adminReviewPublished
    : status === "Rejected" ? messages.adminReviewRejected
      : status === "Hidden" ? messages.adminReviewHidden
        : messages.adminReviewPending;

export function AccountReviews() {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const [reviews, setReviews] = useState<MyReviews | null>(null);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [rating, setRating] = useState(5);
  const [body, setBody] = useState("");
  const [showAvatar, setShowAvatar] = useState(true);
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [mine, page] = await Promise.all([
        getMyReviews(),
        getCatalogProducts({ pageSize: 100 }).catch(() => null),
      ]);
      setReviews(mine);
      setProducts(page?.items ?? []);
      const store = mine.storeReview[0];
      if (store) {
        setRating(store.rating);
        setBody(store.body);
        setShowAvatar(store.showAvatar);
      }
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (status === "authenticated") void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  async function saveStoreReview() {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await upsertStoreReview({ rating, title: null, body, showAvatar });
      setNotice(messages.reviewsSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function removeStoreReview() {
    setSaving(true);
    setError(null);
    try {
      await deleteStoreReview();
      setRating(5);
      setBody("");
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <main className="mx-auto w-full max-w-4xl px-5 py-10"><LoadingCard /></main>;

  return (
    <AccountShell>
      <PageIntro eyebrow="RIGX / REVIEWS" title={messages.reviewsMyTitle} lead={messages.reviewsMyLead} />
      {!user ? <Message tone="info">{messages.accountRedirecting}</Message> : null}
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card p-5">
        <h2 className="text-lg font-bold text-[var(--ds-text)]">{messages.reviewStoreTitle}</h2>
        <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{messages.reviewStoreLead}</p>
        {reviews?.storeReview[0] ? (
          <p className="mt-3 text-xs font-bold text-[var(--ds-text-muted)]">
            {messages.reviewsStatus}: {statusLabel(reviews.storeReview[0].status, messages)}
          </p>
        ) : null}
        <div className="mt-4 grid gap-4">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-semibold text-[var(--ds-text)]">{messages.reviewsYourRating}</span>
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                onClick={() => setRating(star)}
                aria-label={`${messages.reviewsYourRating} ${star} / 5`}
                aria-pressed={rating === star}
                className={`text-2xl ${star <= rating ? "text-[var(--ds-accent)]" : "text-[var(--ds-border)]"}`}
              >
                ★
              </button>
            ))}
          </div>
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.reviewsBody}
            <textarea
              value={body}
              onChange={(event) => setBody(event.target.value)}
              rows={4}
              minLength={10}
              maxLength={4000}
              className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal"
            />
          </label>
          <label className="flex items-center gap-2 text-sm text-[var(--ds-text)]">
            <input type="checkbox" checked={showAvatar} onChange={(event) => setShowAvatar(event.target.checked)} className="h-4 w-4" />
            {messages.reviewsShowAvatar}
          </label>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void saveStoreReview()} pending={saving} disabled={body.trim().length < 10}>
              {messages.reviewStoreSubmit}
            </PrimaryButton>
            {reviews?.storeReview[0] ? (
              <SecondaryButton type="button" onClick={() => void removeStoreReview()} disabled={saving}>{messages.reviewsDelete}</SecondaryButton>
            ) : null}
          </div>
        </div>
      </section>

      <section className="grid gap-4">
        <h2 className="text-lg font-bold text-[var(--ds-text)]">{messages.reviewsMyTitle}</h2>
        {!reviews || reviews.productReviews.length === 0 ? (
          <Message tone="info">{messages.reviewsEmpty}</Message>
        ) : (
          reviews.productReviews.map((review) => {
            const product = products.find((item) => item.id === review.productId);
            return (
              <article key={review.id} className="rigx-admin-card flex flex-wrap items-center justify-between gap-3 p-4">
                <div className="min-w-0">
                  <p className="font-bold text-[var(--ds-text)]">
                    {product ? (
                      <Link href={`/products/${encodeURIComponent(product.slug)}`} className="hover:text-[var(--ds-accent-strong)]">{product.name}</Link>
                    ) : review.productId}
                  </p>
                  <p className="text-xs font-semibold text-[var(--ds-text-muted)]" dir="ltr">{"★".repeat(review.rating)}</p>
                  <p className="mt-1 line-clamp-2 text-sm text-[var(--ds-text-muted)]">{review.body}</p>
                </div>
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {statusLabel(review.status, messages)}
                </span>
              </article>
            );
          })
        )}
      </section>
      {!loading && reviews?.productReviews.length === 0 ? (
        <Link href="/products" className="rigx-button rigx-button-secondary w-fit">{messages.wishlistBrowse}</Link>
      ) : null}
    </AccountShell>
  );
}

