"use client";

import { useState } from "react";
import { requestEmailChange } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { Message, PrimaryButton, TextField } from "@/components/ui";

export function EmailChangeForm() {
  const { messages } = useLocale();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newEmail, setNewEmail] = useState("");
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    setPending(true);
    setError(null);
    setNotice(null);
    try {
      await requestEmailChange({ currentPassword, newEmail });
      setNotice(messages.emailChangeSent);
      setCurrentPassword("");
      setNewEmail("");
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="rigx-admin-card p-5">
      <h2 className="text-lg font-bold text-[var(--ds-text)]">{messages.emailChangeTitle}</h2>
      <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{messages.emailChangeLead}</p>
      {notice ? <div className="mt-3"><Message tone="success">{notice}</Message></div> : null}
      {error ? <div className="mt-3"><Message tone="error">{error}</Message></div> : null}
      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <TextField
          label={messages.emailChangeCurrentPassword}
          type="password"
          autoComplete="current-password"
          value={currentPassword}
          onChange={(event) => setCurrentPassword(event.target.value)}
        />
        <TextField
          label={messages.emailChangeNewEmail}
          type="email"
          dir="ltr"
          value={newEmail}
          onChange={(event) => setNewEmail(event.target.value)}
        />
      </div>
      <PrimaryButton
        type="button"
        className="mt-4"
        pending={pending}
        disabled={!currentPassword || !newEmail}
        onClick={() => void submit()}
      >
        {messages.emailChangeSubmit}
      </PrimaryButton>
    </section>
  );
}
