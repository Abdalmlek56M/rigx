"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { formatDateTime } from "@/lib/format";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { fetchSessions } from "./account-api";
import { LogoutIcon } from "@/components/ui/icons";
import { ConfirmButton } from "./ConfirmButton";
import { LoadBoundary } from "./LoadBoundary";
import { useLoad } from "@/lib/hooks/use-load";

/** The devices signed in to this account: sign one out, or all the others at once. */
export function SessionsList() {
  const t = useTranslations("account.profile.sessions");
  const locale = useLocale();
  const { state, reload } = useLoad(fetchSessions);
  const [failed, setFailed] = useState(false);

  async function run(action: () => Promise<unknown>) {
    setFailed(false);
    try {
      await action();
      reload();
    } catch {
      setFailed(true);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <h3 className="m-0 type-section-title">{t("title")}</h3>
      {failed ? <p role="alert" className="m-0 text-sm text-error-text">{t("failed")}</p> : null}
      <LoadBoundary state={state} reload={reload}>
        {(sessions) => (
          <>
            <ul className="m-0 list-none p-0">
              {sessions.map((session) => (
                <li key={session.id} className="flex flex-wrap items-center justify-between gap-2 border-b border-line py-2 last:border-b-0">
                  <div className="flex flex-col gap-0.5">
                    <span className="flex flex-wrap items-center gap-2 text-sm font-bold">
                      {session.browser} — {session.device}
                      {session.isCurrent ? <StatusBadge tone="info">{t("current")}</StatusBadge> : null}
                    </span>
                    <span className="text-sm text-ink-muted">{t("lastActive")} {formatDateTime(session.lastActivityAtUtc, locale)}</span>
                  </div>
                  {session.isCurrent ? null : (
                    <ConfirmButton iconOnly label={t("signOut")} icon={LogoutIcon} onConfirm={() => void run(() => unwrap(browserApi().DELETE("/api/v1/identity/sessions/{id}", { params: { path: { id: session.id } } })))} />
                  )}
                </li>
              ))}
            </ul>
            {sessions.some((session) => !session.isCurrent) ? (
              <div>
                <ConfirmButton label={t("signOutOthers")} icon={LogoutIcon} onConfirm={() => void run(() => unwrap(browserApi().DELETE("/api/v1/identity/sessions/others")))} />
              </div>
            ) : null}
          </>
        )}
      </LoadBoundary>
    </div>
  );
}
