"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  getIdentitySessions,
  revokeIdentitySession,
  revokeOtherIdentitySessions,
  type IdentitySession,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { AccountShell } from "@/components/account-shell";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, TextLink } from "@/components/ui";

export function AccountSessions() {
  const { messages, locale } = useLocale();
  const { status, user, markSignedOut, refresh } = useSession();
  const router = useRouter();
  const [sessions, setSessions] = useState<IdentitySession[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setSessions(await getIdentitySessions());
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

  if (status === "loading") return <LoadingCard />;
  if (status === "unavailable") {
    return (
      <main className="mx-auto w-full max-w-3xl px-5 py-10">
        <Message tone="error">{messages.accountUnavailable}</Message>
        <SecondaryButton type="button" onClick={() => void refresh()}>{messages.retry}</SecondaryButton>
      </main>
    );
  }
  if (!user) {
    return (
      <main className="mx-auto w-full max-w-3xl px-5 py-10">
        <Message tone="info">{messages.accountRedirecting}</Message>
        <TextLink href="/login">{messages.navLogin}</TextLink>
      </main>
    );
  }

  async function revoke(session: IdentitySession) {
    setBusy(session.id);
    setError(null);
    setNotice(null);
    try {
      await revokeIdentitySession(session.id);
      if (session.isCurrent) {
        markSignedOut();
        router.replace("/login?notice=signed-out");
        return;
      }
      setNotice(messages.accountSessionRevoked);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setBusy(null);
    }
  }

  async function revokeOthers() {
    setBusy("others");
    setError(null);
    setNotice(null);
    try {
      await revokeOtherIdentitySessions();
      setNotice(messages.accountOtherSessionsRevoked);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setBusy(null);
    }
  }

  return (
    <AccountShell>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow={messages.accountEyebrow} title={messages.accountSessionsTitle} lead={messages.accountSessionsLead} />
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      {loading ? <LoadingCard /> : (
        <section className="rigx-admin-card">
          <div className="rigx-admin-card-head">
            <h2>{messages.accountSessionsTitle}</h2>
            <SecondaryButton type="button" onClick={() => void revokeOthers()} disabled={busy !== null}>
              {messages.accountRevokeOtherSessions}
            </SecondaryButton>
          </div>
          <div className="grid gap-3 p-4">
            {sessions.map((session) => (
              <article key={session.id} className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p className="font-bold text-[var(--ds-text)]">
                      {session.device} · {session.browser}
                      {session.isCurrent ? <span className="ms-2 rounded-full bg-[var(--ds-accent-soft)] px-2 py-0.5 text-xs text-[var(--ds-accent-strong)]">{messages.accountCurrentSession}</span> : null}
                    </p>
                    <p className="mt-1 text-xs text-[var(--ds-text-muted)]">
                      {messages.accountLastActivity}: {new Date(session.lastActivityAtUtc).toLocaleString(locale === "ar" ? "ar-EG" : "en-GB")}
                    </p>
                    <p className="text-xs text-[var(--ds-text-muted)]">
                      {messages.accountSignedInAt}: {new Date(session.createdAtUtc).toLocaleString(locale === "ar" ? "ar-EG" : "en-GB")}
                    </p>
                  </div>
                  <SecondaryButton type="button" onClick={() => void revoke(session)} disabled={busy !== null}>
                    {session.isCurrent ? messages.logoutCurrent : messages.accountRevokeSession}
                  </SecondaryButton>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}
    </AccountShell>
  );
}

