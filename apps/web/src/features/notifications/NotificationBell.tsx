"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { unwrap, type Schemas } from "@rigx/api-client";
import { HEADER_ICON_BUTTON } from "@/components/layout/nav-links";
import { Button, IconButton } from "@/components/ui/Button";
import { CountBadge } from "@/components/ui/CountBadge";
import { cardClass } from "@/components/ui/Card";
import { BellIcon, CheckIcon, RefreshIcon } from "@/components/ui/icons";
import { useSession } from "@/features/auth/use-session";
import { Link } from "@/i18n/navigation";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { formatDateTime } from "@/lib/format";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import { notificationHref, notificationText, unreadBadge } from "./notification-rules";

type Feed = Schemas["CustomerNotificationFeed"];
type Entry = Schemas["CustomerNotificationView"];

const fetchFeed = () => required(unwrap(browserApi().GET("/api/v1/account/notifications")));

/** The header bell, only for signed-in visitors. */
export function NotificationBell() {
  const session = useSession();
  return session.status === "signedIn" ? <Bell /> : null;
}

/**
 * Bell with the unread count; the panel lists the newest entries (order status changes, back-in-stock). Opening an entry
 * marks it read. Read when the page loads and again each time the panel opens (no polling).
 */
function Bell() {
  const t = useTranslations("notifications");
  const [feed, setFeed] = useState<Feed | null>(null);
  const [failed, setFailed] = useState(false);
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const close = useCallback(() => setOpen(false), []);
  useDismiss(ref, open, close);

  const refresh = useCallback(
    () => fetchFeed().then((next) => { setFeed(next); setFailed(false); }, () => setFailed(true)),
    []
  );
  useEffect(() => {
    void refresh();
  }, [refresh]);

  function toggle() {
    if (!open) void refresh();
    setOpen(!open);
  }

  async function markAllRead() {
    await unwrap(browserApi().POST("/api/v1/account/notifications/read-all")).catch(() => undefined);
    await refresh();
  }

  function opened(entry: Entry) {
    setOpen(false);
    if (entry.read) return;
    void unwrap(browserApi().POST("/api/v1/account/notifications/{id}/read", { params: { path: { id: entry.id } } }))
      .catch(() => undefined)
      .then(refresh);
  }

  const unread = feed?.unreadCount ?? 0;
  const badge = unreadBadge(unread);

  return (
    <div ref={ref} className="relative">
      <button
        type="button" aria-expanded={open} aria-haspopup="true" onClick={toggle}
        aria-label={unread > 0 ? t("bellUnread", { count: unread }) : t("bell")}
        className={`relative ${HEADER_ICON_BUTTON}`}
      >
        <BellIcon />
        {badge ? (
          <CountBadge>{badge}</CountBadge>
        ) : null}
      </button>
      {open ? (
        <div className={`absolute end-0 top-full z-50 mt-2 flex w-[min(360px,calc(100vw-32px))] flex-col ${cardClass({ padding: "none" })} text-ink shadow-soft`}>
          <div className="flex items-center justify-between gap-3 border-b border-line px-4 py-3">
            <h2 className="m-0 type-section-title">{t("title")}</h2>
            {unread > 0 ? (
              <IconButton icon={CheckIcon} label={t("markAll")} variant="ghost" size="sm" onClick={() => void markAllRead()} />
            ) : null}
          </div>
          <Entries feed={feed} failed={failed} onOpen={opened} onRetry={() => void refresh()} />
        </div>
      ) : null}
    </div>
  );
}

function Entries({ feed, failed, onOpen, onRetry }: { feed: Feed | null; failed: boolean; onOpen: (entry: Entry) => void; onRetry: () => void }) {
  const t = useTranslations("notifications");
  const locale = useLocale();

  if (failed && !feed) {
    return (
      <p role="alert" className="m-0 flex flex-wrap items-center gap-2 px-4 py-5 text-sm text-ink-muted">
        {t("failed")}
        <Button variant="ghost" size="sm" icon={RefreshIcon} onClick={onRetry}>{t("retry")}</Button>
      </p>
    );
  }
  if (!feed) return <p className="m-0 px-4 py-5 text-sm text-ink-muted">{t("loading")}</p>;
  if (feed.items.length === 0) return <p className="m-0 px-4 py-5 text-sm text-ink-muted">{t("empty")}</p>;

  return (
    <ul className="m-0 max-h-[min(420px,70vh)] list-none overflow-y-auto p-1.5">
      {feed.items.map((entry) => {
        const text = notificationText(entry);
        const href = notificationHref(entry);
        const body = (
          <>
            <span aria-hidden="true" className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${entry.read ? "bg-transparent" : "bg-orange"}`} />
            <span className="flex min-w-0 flex-col gap-0.5">
              <span dir="auto" className={`text-sm leading-snug [unicode-bidi:plaintext] ${entry.read ? "text-ink-muted" : "font-bold"}`}>
                {entry.read ? null : <span className="sr-only">{t("unread")} </span>}
                {t(text.key, text.values)}
              </span>
              <span className="text-xs text-ink-muted">{formatDateTime(entry.createdAtUtc, locale)}</span>
            </span>
          </>
        );
        const row = "flex w-full items-start gap-2.5 rounded-control px-2.5 py-2.5 text-start";
        return (
          <li key={entry.id}>
            {href ? (
              <Link href={href} onClick={() => onOpen(entry)} className={`${row} hover:bg-paper`}>{body}</Link>
            ) : (
              <div className={row}>{body}</div>
            )}
          </li>
        );
      })}
    </ul>
  );
}
