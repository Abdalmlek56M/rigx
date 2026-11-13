"use client";

import { useEffect, useState } from "react";
import { getAdminReviews, moderateReview, type AdminReview } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

export function ContentReviewsAdmin() {
  const { messages } = useLocale();
  const [reviews, setReviews] = useState<AdminReview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("Pending");
  const [typeFilter, setTypeFilter] = useState("");
  const [selected, setSelected] = useState<AdminReview | null>(null);
  const [reason, setReason] = useState("");
  const [pending, setPending] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const page = await getAdminReviews({ status: statusFilter || undefined, type: typeFilter || undefined, pageSize: 100 });
      setReviews(page.items);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter, typeFilter]);

  async function moderate(review: AdminReview, action: "Publish" | "Reject" | "Hide") {
    setPending(true);
    setError(null);
    try {
      await moderateReview(review.type, review.id, action, review.rowVersion, reason.trim() || undefined);
      setNotice(action === "Publish" ? messages.adminPublished : messages.adminSaved);
      setSelected(null);
      setReason("");
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  const statusLabel = (status: string) =>
    status === "Published" ? messages.adminReviewPublished
      : status === "Rejected" ? messages.adminReviewRejected
        : status === "Hidden" ? messages.adminReviewHidden
          : messages.adminReviewPending;

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-5xl gap-6 px-4 py-8 sm:px-6">
      <PageIntro eyebrow="RIGX / MODERATION" title={messages.adminReviewsTitle} lead={messages.adminReviewsLead} />
      <div className="flex flex-wrap gap-2">
        {["Pending", "Published", "Rejected", "Hidden", ""].map((status) => (
          <button
            key={status || "all"}
            type="button"
            onClick={() => setStatusFilter(status)}
            aria-pressed={statusFilter === status}
            className={`rounded-full border px-3 py-1.5 text-sm font-semibold ${
              statusFilter === status
                ? "border-[var(--ds-accent)] bg-[var(--ds-accent-soft)] text-[var(--ds-accent-strong)]"
                : "border-[var(--ds-border)] bg-[var(--ds-surface)] text-[var(--ds-text)]"
            }`}
          >
            {status ? statusLabel(status) : messages.adminMediaSearch}
          </button>
        ))}
        <span className="mx-2 self-center text-[var(--ds-border)]">|</span>
        {["", "Product", "Store"].map((type) => (
          <button
            key={type || "all-types"}
            type="button"
            onClick={() => setTypeFilter(type)}
            aria-pressed={typeFilter === type}
            className={`rounded-full border px-3 py-1.5 text-sm font-semibold ${
              typeFilter === type
                ? "border-[var(--ds-accent)] bg-[var(--ds-accent-soft)] text-[var(--ds-accent-strong)]"
                : "border-[var(--ds-border)] bg-[var(--ds-surface)] text-[var(--ds-text)]"
            }`}
          >
            {type === "Product" ? messages.adminReviewTypeProduct : type === "Store" ? messages.adminReviewTypeStore : "All"}
          </button>
        ))}
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : reviews.length === 0 ? (
        <Message tone="info">{messages.adminNoReviews}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {reviews.map((review) => (
            <article key={`${review.type}-${review.id}`} className="flex flex-wrap items-start justify-between gap-3 p-4">
              <div className="min-w-0 max-w-2xl">
                <p className="text-xs font-bold text-[var(--ds-accent-strong)]">
                  {review.type === "Product" ? messages.adminReviewTypeProduct : messages.adminReviewTypeStore} · {"★".repeat(review.rating)}
                </p>
                {review.title ? <p className="mt-1 font-bold text-[var(--ds-text)]">{review.title}</p> : null}
                <p className="mt-1 text-sm leading-6 text-[var(--ds-text-muted)]">{review.body}</p>
                <p className="mt-1 text-xs text-[var(--ds-text-muted)]" dir="ltr">{new Date(review.updatedAtUtc).toLocaleString("en-GB")}</p>
              </div>
              <div className="flex flex-col items-end gap-2">
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {statusLabel(review.status)}
                </span>
                <SecondaryButton type="button" onClick={() => { setSelected(review); setReason(""); }}>{messages.auditDetails}</SecondaryButton>
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer open={selected !== null} title={messages.adminReviewsTitle} closeLabel={messages.adminCancel} onClose={() => setSelected(null)}>
        {selected ? (
          <div className="grid gap-4">
            <p className="text-sm leading-7 text-[var(--ds-text-muted)]">{selected.body}</p>
            <TextField label={messages.adminReviewReason} value={reason} onChange={(event) => setReason(event.target.value)} />
            <div className="flex flex-wrap gap-2">
              <PrimaryButton type="button" pending={pending} onClick={() => void moderate(selected, "Publish")}>{messages.adminReviewPublish}</PrimaryButton>
              <SecondaryButton type="button" disabled={pending} onClick={() => void moderate(selected, "Reject")}>{messages.adminReviewReject}</SecondaryButton>
              <SecondaryButton type="button" disabled={pending} onClick={() => void moderate(selected, "Hide")}>{messages.adminReviewHide}</SecondaryButton>
            </div>
          </div>
        ) : null}
      </SideDrawer>
    </main>
  );
}
