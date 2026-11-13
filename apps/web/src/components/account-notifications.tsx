"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getNotifications, markAllNotificationsRead, markNotificationRead, type NotificationItem } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { AccountShell } from "@/components/account-shell";
import { LoadingCard, Message, PageIntro, SecondaryButton } from "@/components/ui";

export function AccountNotifications() {
  const { messages } = useLocale();
  const { status } = useSession();
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    try {
      const page = await getNotifications();
      setItems(page.items);
    } catch {
      setItems([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (status === "authenticated") void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  const label = (item: NotificationItem) => {
    const map: Record<string, string> = {
      "notifications.reviewPublished": messages.notificationReviewPublished,
      "notifications.reviewRejected": messages.notificationReviewRejected,
      "notifications.reviewHidden": messages.notificationReviewHidden,
      "notifications.reviewUpdated": messages.notificationReviewUpdated,
    };
    return map[item.titleKey] ?? messages.notificationsTitle;
  };

  return (
    <AccountShell>
      <div className="flex flex-wrap items-end justify-between gap-3">
        <PageIntro eyebrow="RIGX / ACCOUNT" title={messages.notificationsTitle} lead={messages.notificationsLead} />
        <SecondaryButton type="button" onClick={() => void markAllNotificationsRead().then(load)}>{messages.notificationsMarkAll}</SecondaryButton>
      </div>
      {loading ? <LoadingCard /> : items.length === 0 ? (
        <Message tone="info">{messages.notificationsEmpty}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {items.map((item) => (
            <article key={item.id} className={`flex flex-wrap items-center justify-between gap-3 p-4 ${item.isRead ? "opacity-70" : ""}`}>
              <div>
                <p className="font-semibold text-[var(--ds-text)]">{label(item)}</p>
                <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{new Date(item.createdAtUtc).toLocaleString("en-GB")}</p>
              </div>
              <div className="flex items-center gap-2">
                {item.linkUrl ? <Link href={item.linkUrl} className="text-sm font-semibold text-[var(--ds-accent)]" onClick={() => void markNotificationRead(item.id)}>{messages.notificationsOpen}</Link> : null}
                {!item.isRead ? (
                  <SecondaryButton type="button" onClick={() => void markNotificationRead(item.id).then(load)}>{messages.notificationsMarkRead}</SecondaryButton>
                ) : null}
              </div>
            </article>
          ))}
        </section>
      )}
    </AccountShell>
  );
}

