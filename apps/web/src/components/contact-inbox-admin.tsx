"use client";

import { useEffect, useState } from "react";
import { getContactMessages, updateContactStatus, type ContactMessageRecord } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, SecondaryButton } from "@/components/ui";

export function ContactInboxAdmin() {
  const { messages } = useLocale();
  const [items, setItems] = useState<ContactMessageRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    try { setItems((await getContactMessages()).items); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  async function setStatus(item: ContactMessageRecord, status: string) {
    try {
      const updated = await updateContactStatus(item.id, status, item.rowVersion);
      setItems((current) => current.map((entry) => entry.id === updated.id ? updated : entry));
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }

  return (
    <main className="p-4 sm:p-6 lg:p-8"><div className="mx-auto grid max-w-5xl gap-5">
      <PageIntro eyebrow="RIGX / CONTACT" title={messages.adminContactTitle} lead={messages.adminContactLead} />
      {error ? <Message tone="error">{error}</Message> : null}
      {loading ? <LoadingCard /> : items.length === 0 ? <Message tone="info">{messages.adminContactEmpty}</Message> : (
        <section className="grid gap-3">
          {items.map((item) => (
            <article key={item.id} className="rigx-admin-card grid gap-2 p-4">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <strong>{item.subject} · {item.fullName}</strong>
                <span className="text-xs font-bold">{item.status}</span>
              </div>
              <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{item.email}{item.phone ? ` · ${item.phone}` : ""}</p>
              <p className="text-sm whitespace-pre-wrap">{item.message}</p>
              <div className="flex flex-wrap gap-2">
                {["New", "Read", "Closed"].filter((status) => status !== item.status).map((status) => (
                  <SecondaryButton key={status} type="button" onClick={() => void setStatus(item, status)}>{status}</SecondaryButton>
                ))}
              </div>
            </article>
          ))}
        </section>
      )}
    </div></main>
  );
}
