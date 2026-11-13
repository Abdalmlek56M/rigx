"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { updateIdentityProfile } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { ProfileAvatar } from "@/components/profile-avatar";
import { AccountShell } from "@/components/account-shell";
import { EmailChangeForm } from "@/components/email-change-form";
import {
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  TextField,
  TextLink,
} from "@/components/ui";

export function AccountProfile() {
  const { messages } = useLocale();
  const { status, user, refresh } = useSession();
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    if (!user) return;
    setFullName(user.fullName);
    setPhoneNumber(user.phoneNumber ?? "");
  }, [user]);

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

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await updateIdentityProfile({
        fullName,
        phoneNumber: phoneNumber.trim() || null,
      });
      await refresh();
      setNotice(messages.accountProfileSaved);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  return (
    <AccountShell>
      <PageIntro eyebrow={messages.accountEyebrow} title={messages.accountProfileTitle} lead={messages.accountProfileLead} />
      <section className="rigx-admin-card p-5">
        <ProfileAvatar size="lg" editable />
      </section>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      <section className="rigx-admin-card">
        <div className="rigx-admin-card-head">
          <h2>{messages.accountProfileTitle}</h2>
          <Link href="/account/security" className="text-sm font-semibold text-[var(--ds-accent)]">
            {messages.accountSecurityTitle}
          </Link>
        </div>
        <div className="grid gap-4 p-4 sm:grid-cols-2">
          <div className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3 text-sm">
            <p className="text-xs font-bold text-[var(--ds-text-muted)]">{messages.username}</p>
            <p className="mt-1 font-mono" dir="ltr">{user.username}</p>
          </div>
          <div className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3 text-sm">
            <p className="text-xs font-bold text-[var(--ds-text-muted)]">{messages.email}</p>
            <p className="mt-1 break-all" dir="ltr">{user.email}</p>
            <p className="mt-2 text-xs text-[var(--ds-text-muted)]">
              {user.isEmailVerified ? messages.adminIdentityVerified : messages.adminIdentityNotVerified}
            </p>
          </div>
          <TextField
            label={messages.adminIdentityFullName}
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            required
          />
          <TextField
            label={messages.adminIdentityPhone}
            value={phoneNumber}
            onChange={(event) => setPhoneNumber(event.target.value)}
            inputMode="tel"
            dir="ltr"
          />
        </div>
        <div className="flex flex-wrap gap-2 p-4 pt-0">
          <PrimaryButton type="button" onClick={() => void save()} pending={saving}>
            {messages.adminSaveChanges}
          </PrimaryButton>
          <Link href="/account/sessions" className="rigx-button rigx-button-secondary">
            {messages.accountSessionsTitle}
          </Link>
          <Link href="/account/addresses" className="rigx-button rigx-button-secondary">
            {messages.addressTitle}
          </Link>
          <Link href="/account/wishlist" className="rigx-button rigx-button-secondary">
            {messages.wishlistTitle}
          </Link>
        </div>
      </section>
      <EmailChangeForm />
    </AccountShell>
  );
}

