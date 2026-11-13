"use client";

import { useEffect, useState } from "react";
import { getAdminDashboard, type AdminDashboardSnapshot } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message } from "@/components/ui";

function barHeight(value: number, maximum: number) {
  const ratio = maximum <= 0 ? 0 : value / maximum;
  return `${Math.max(3, Math.round(ratio * 100))}%`;
}

export function AdminAnalytics() {
  const { messages } = useLocale();
  const [data, setData] = useState<AdminDashboardSnapshot | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void getAdminDashboard()
      .then((snapshot) => { if (active) setData(snapshot); })
      .catch((requestError) => { if (active) setError(apiErrorMessage(requestError, messages)); });
    return () => { active = false; };
  }, [messages]);

  if (error) return <Message tone="error">{error}</Message>;
  if (!data) return <LoadingCard />;

  const groups = [
    { title: messages.adminGroupCatalog, items: data.catalog },
    { title: messages.adminGroupPurchasing, items: data.purchasing },
    { title: messages.adminGroupIdentity, items: data.customers },
    { title: messages.adminGroupStorefront, items: data.content },
    { title: messages.reviewsTitle, items: data.reviews },
    { title: messages.adminMediaLibraryNav, items: data.media },
  ];
  const accountMax = Math.max(1, ...data.accountsLast14Days.map((point) => point.value));
  const reviewMax = Math.max(1, ...data.reviewsLast14Days.map((point) => point.value));

  return (
    <section className="mt-8 grid gap-5" aria-label={messages.dashboardAnalyticsTitle}>
      <div>
        <h2 className="text-2xl font-black text-[var(--ds-text)]">{messages.dashboardAnalyticsTitle}</h2>
        <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{messages.dashboardAnalyticsLead}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {groups.map((group) => (
          <article key={group.title} className="rigx-admin-card p-4">
            <h3 className="text-sm font-black text-[var(--ds-accent-strong)]">{group.title}</h3>
            <dl className="mt-3 grid gap-2">
              {group.items.map((item) => (
                <div key={item.key} className="flex items-center justify-between gap-2 text-sm">
                  <dt className="text-[var(--ds-text-muted)]">{item.key}</dt>
                  <dd className="font-black text-[var(--ds-text)]">{item.value.toLocaleString()}</dd>
                </div>
              ))}
            </dl>
          </article>
        ))}
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <article className="rigx-admin-card p-4">
          <h3 className="text-sm font-black text-[var(--ds-text)]">{messages.dashboardAccountsChart}</h3>
          <div className="mt-4 flex h-40 items-end gap-1" role="img" aria-label={messages.dashboardAccountsChart}>
            {data.accountsLast14Days.map((point) => (
              <div key={point.date} className="flex flex-1 flex-col items-center justify-end gap-1">
                <span className="w-full rounded-t bg-[var(--ds-accent)]" style={{ height: barHeight(point.value, accountMax) }} title={`${point.date}: ${point.value}`} />
              </div>
            ))}
          </div>
        </article>
        <article className="rigx-admin-card p-4">
          <h3 className="text-sm font-black text-[var(--ds-text)]">{messages.dashboardReviewsChart}</h3>
          <div className="mt-4 flex h-40 items-end gap-1" role="img" aria-label={messages.dashboardReviewsChart}>
            {data.reviewsLast14Days.map((point) => (
              <div key={point.date} className="flex flex-1 flex-col items-center justify-end gap-1">
                <span className="w-full rounded-t bg-[var(--ds-accent)]" style={{ height: barHeight(point.value, reviewMax) }} title={`${point.date}: ${point.value}`} />
              </div>
            ))}
          </div>
        </article>
        <article className="rigx-admin-card p-4">
          <h3 className="text-sm font-black text-[var(--ds-text)]">{messages.dashboardRatingDistribution}</h3>
          <div className="mt-3 grid gap-2">
            {data.ratingDistribution.map((row) => (
              <div key={row.label} className="grid grid-cols-[3rem_1fr_2rem] items-center gap-2 text-sm">
                <span dir="ltr">{row.label}</span>
                <span className="h-2 rounded-full bg-[var(--ds-surface-muted)]">
                  <span className="block h-2 rounded-full bg-[var(--ds-accent)]" style={{ width: `${Math.min(100, row.value * 10)}%` }} />
                </span>
                <span className="text-end font-bold">{row.value}</span>
              </div>
            ))}
          </div>
        </article>
        <article className="rigx-admin-card p-4">
          <h3 className="text-sm font-black text-[var(--ds-text)]">{messages.dashboardPurchaseSources}</h3>
          {data.draftPurchaseSources.length === 0 ? <p className="mt-3 text-sm text-[var(--ds-text-muted)]">{messages.dashboardNoData}</p> : (
            <ul className="mt-3 grid gap-2 text-sm">
              {data.draftPurchaseSources.map((row) => (
                <li key={row.label} className="flex justify-between gap-2">
                  <span className="text-[var(--ds-text-muted)]">{row.label}</span>
                  <span className="font-bold">{row.value}</span>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>

      <article className="rigx-admin-card p-4">
        <h3 className="text-sm font-black text-[var(--ds-text)]">{messages.dashboardRecentActivity}</h3>
        {data.recentAuditEvents.length === 0 ? <p className="mt-3 text-sm text-[var(--ds-text-muted)]">{messages.dashboardNoData}</p> : (
          <ul className="mt-3 grid gap-2 text-sm">
            {data.recentAuditEvents.map((event) => (
              <li key={event.id} className="flex flex-wrap justify-between gap-2 border-b border-[var(--ds-border)] pb-2 last:border-0">
                <span className="font-semibold" dir="ltr">{event.eventType}</span>
                <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{new Date(event.occurredAtUtc).toLocaleString("en-GB")} · {event.outcome}</span>
              </li>
            ))}
          </ul>
        )}
      </article>
    </section>
  );
}
