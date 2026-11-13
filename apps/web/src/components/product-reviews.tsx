"use client";

import { useEffect, useState } from "react";
import {
  catalogMediaUrl,
  deleteProductReview,
  getMyReviews,
  getProductReviews,
  upsertProductReview,
  type MyProductReview,
  type PublicProductReview,
  type ReviewAggregate,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { ConfirmDialog, LoadingCard, Message, PrimaryButton, SecondaryButton } from "@/components/ui";

function Stars({ value, className = "" }: { value: number; className?: string }) {
  return (
    <span className={`inline-flex items-center gap-0.5 ${className}`} dir="ltr" aria-label={`${value} / 5`}>
      {[1, 2, 3, 4, 5].map((star) => (
        <span key={star} aria-hidden="true" className={star <= Math.round(value) ? "text-[var(--ds-accent)]" : "text-[var(--ds-border)]"}>★</span>
      ))}
    </span>
  );
}

export function ProductReviews({ productId }: { productId: string }) {
  const { messages, locale } = useLocale();
  const { status } = useSession();
  const [aggregate, setAggregate] = useState<ReviewAggregate | null>(null);
  const [items, setItems] = useState<PublicProductReview[]>([]);
  const [mine, setMine] = useState<MyProductReview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [rating, setRating] = useState(5);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [showAvatar, setShowAvatar] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const publicResponse = await getProductReviews(productId);
      setAggregate(publicResponse.aggregate);
      setItems(publicResponse.items);
      if (status === "authenticated") {
        const mineResponse = await getMyReviews();
        const own = mineResponse.productReviews.find((review) => review.productId === productId) ?? null;
        setMine(own);
        if (own) {
          setRating(own.rating);
          setTitle(own.title ?? "");
          setBody(own.body);
          setShowAvatar(own.showAvatar);
        }
      }
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [productId, status]);

  async function submit() {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await upsertProductReview(productId, {
        rating,
        title: title.trim() || null,
        body,
        showAvatar,
      });
      setNotice(messages.reviewsSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function remove() {
    setDeleting(true);
    setError(null);
    try {
      await deleteProductReview(productId);
      setConfirmDelete(false);
      setMine(null);
      setRating(5);
      setTitle("");
      setBody("");
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setDeleting(false);
    }
  }

  return (
    <section className="mt-14 border-t border-[var(--ds-border)] pt-10" aria-labelledby="product-reviews-heading">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="rigx-section-kicker">RIGX / REVIEWS</p>
          <h2 id="product-reviews-heading" className="mt-1 text-2xl font-bold text-[var(--ds-text)]">{messages.reviewsTitle}</h2>
        </div>
        {aggregate && aggregate.publishedCount > 0 ? (
          <div className="flex items-center gap-2 text-sm text-[var(--ds-text-muted)]">
            <Stars value={aggregate.averageRating} className="text-lg" />
            <span dir="ltr">{aggregate.averageRating.toFixed(1)} · {messages.reviewsCount.replace("{count}", String(aggregate.publishedCount))}</span>
          </div>
        ) : null}
      </div>

      {loading ? <div className="mt-6"><LoadingCard /></div> : null}
      {error ? <div className="mt-4"><Message tone="error">{error}</Message></div> : null}
      {notice ? <div className="mt-4"><Message tone="success">{notice}</Message></div> : null}

      {!loading && items.length === 0 ? <p className="mt-5 text-sm text-[var(--ds-text-muted)]">{messages.reviewsEmpty}</p> : null}
      {items.length > 0 ? (
        <div className="mt-6 grid gap-4">
          {items.map((review) => (
            <article key={review.id} className="rigx-admin-card p-5">
              <div className="flex flex-wrap items-center gap-3">
                {review.authorAvatarStorageKey ? (
                  <img src={catalogMediaUrl(review.authorAvatarStorageKey)} alt="" className="h-9 w-9 rounded-full border border-[var(--ds-border)] object-cover" />
                ) : (
                  <span aria-hidden="true" className="grid h-9 w-9 place-items-center rounded-full bg-[var(--ds-accent-soft)] text-xs font-black text-[var(--ds-accent-strong)]">
                    {review.authorName.charAt(0).toUpperCase()}
                  </span>
                )}
                <div>
                  <p className="text-sm font-bold text-[var(--ds-text)]" dir="auto">{review.authorName}</p>
                  <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{new Date(review.createdAtUtc).toLocaleDateString(locale === "ar" ? "ar-EG" : "en-GB")}</p>
                </div>
                <Stars value={review.rating} className="ms-auto" />
              </div>
              {review.title ? <p className="mt-3 font-bold text-[var(--ds-text)]">{review.title}</p> : null}
              <p className="mt-2 whitespace-pre-line text-sm leading-7 text-[var(--ds-text-muted)]">{review.body}</p>
            </article>
          ))}
        </div>
      ) : null}

      <div className="mt-10 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-5">
        <h3 className="text-lg font-bold text-[var(--ds-text)]">{messages.reviewsWrite}</h3>
        {status !== "authenticated" ? (
          <p className="mt-2 text-sm text-[var(--ds-text-muted)]">{messages.accountRedirecting}</p>
        ) : (
          <div className="mt-4 grid gap-4">
            {mine ? (
              <p className={`text-xs font-bold ${mine.status === "Published" ? "text-emerald-600" : "text-amber-600"}`}>
                {messages.reviewsStatus}: {mine.status === "Published" ? messages.adminReviewPublished : mine.status === "Pending" ? messages.adminReviewPending : mine.status === "Rejected" ? messages.adminReviewRejected : messages.adminReviewHidden}
                {mine.status === "Pending" ? ` · ${messages.reviewsPendingNotice}` : ""}
              </p>
            ) : null}
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-sm font-semibold text-[var(--ds-text)]">{messages.reviewsYourRating}</span>
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  onClick={() => setRating(star)}
                  aria-label={`${messages.reviewsYourRating} ${star} / 5`}
                  aria-pressed={rating === star}
                  className={`text-2xl transition ${star <= rating ? "text-[var(--ds-accent)]" : "text-[var(--ds-border)]"}`}
                >
                  ★
                </button>
              ))}
            </div>
            <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
              {messages.reviewsTitleLabel}
              <input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                maxLength={120}
                className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm font-normal"
              />
            </label>
            <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
              {messages.reviewsBody}
              <textarea
                value={body}
                onChange={(event) => setBody(event.target.value)}
                rows={5}
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
              <PrimaryButton type="button" onClick={() => void submit()} pending={saving} disabled={body.trim().length < 10}>
                {messages.reviewsSubmit}
              </PrimaryButton>
              {mine ? (
                <SecondaryButton type="button" onClick={() => setConfirmDelete(true)}>{messages.reviewsDelete}</SecondaryButton>
              ) : null}
            </div>
          </div>
        )}
      </div>

      <ConfirmDialog
        open={confirmDelete}
        title={messages.reviewsDelete}
        description={messages.reviewsDeleteConfirm}
        confirmLabel={messages.reviewsDelete}
        cancelLabel={messages.adminCancel}
        pending={deleting}
        onConfirm={() => void remove()}
        onCancel={() => setConfirmDelete(false)}
      />
    </section>
  );
}
