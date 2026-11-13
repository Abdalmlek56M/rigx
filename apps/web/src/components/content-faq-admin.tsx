"use client";

import { useEffect, useState } from "react";
import {
  createFaq,
  getAdminFaqs,
  setFaqPublished,
  updateFaq,
  type ContentFaq,
  type ContentFaqInput,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

const emptyFaq: ContentFaqInput = {
  questionAr: "",
  questionEn: "",
  answerAr: "",
  answerEn: "",
  category: null,
  sortOrder: 0,
};

export function ContentFaqAdmin() {
  const { messages } = useLocale();
  const [faqs, setFaqs] = useState<ContentFaq[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<ContentFaq | null>(null);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<ContentFaqInput>(emptyFaq);
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setFaqs(await getAdminFaqs());
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function openEditor(faq: ContentFaq | null) {
    setEditing(faq);
    setForm(faq ? {
      questionAr: faq.questionAr,
      questionEn: faq.questionEn,
      answerAr: faq.answerAr,
      answerEn: faq.answerEn,
      category: faq.category,
      sortOrder: faq.sortOrder,
    } : emptyFaq);
    setNotice(null);
    setError(null);
    setOpen(true);
  }

  function update<K extends keyof ContentFaqInput>(key: K, value: ContentFaqInput[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function save() {
    setSaving(true);
    setError(null);
    try {
      if (editing) await updateFaq(editing.id, form, editing.rowVersion);
      else await createFaq(form);
      setNotice(messages.adminSaved);
      setOpen(false);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function toggle(faq: ContentFaq) {
    setError(null);
    try {
      await setFaqPublished(faq.id, faq.status !== "Published", faq.rowVersion);
      setNotice(faq.status === "Published" ? messages.adminUnpublished : messages.adminPublished);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-5xl gap-6 px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow="RIGX / CONTENT" title={messages.adminFaqTitle} lead={messages.adminFaqLead} />
        <PrimaryButton type="button" onClick={() => openEditor(null)}>{messages.adminFaqNew}</PrimaryButton>
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : faqs.length === 0 ? (
        <Message tone="info">{messages.adminNoFaqs}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {faqs.map((faq) => (
            <article key={faq.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div className="min-w-0">
                <p className="font-bold text-[var(--ds-text)]">{faq.questionAr}</p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{faq.questionEn}</p>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {faq.status === "Published" ? messages.contentStatusPublished : messages.contentStatusDraft}
                </span>
                <SecondaryButton type="button" onClick={() => openEditor(faq)}>{messages.adminEdit}</SecondaryButton>
                <SecondaryButton type="button" onClick={() => void toggle(faq)}>
                  {faq.status === "Published" ? messages.adminUnpublish : messages.adminPublish}
                </SecondaryButton>
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer open={open} title={messages.adminFaqTitle} closeLabel={messages.adminCancel} onClose={() => setOpen(false)}>
        <div className="grid gap-4">
          <TextField label={messages.adminFaqQuestionAr} value={form.questionAr} onChange={(event) => update("questionAr", event.target.value)} required />
          <TextField label={messages.adminFaqQuestionEn} value={form.questionEn} onChange={(event) => update("questionEn", event.target.value)} dir="ltr" required />
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.adminFaqAnswerAr}
            <textarea
              value={form.answerAr}
              onChange={(event) => update("answerAr", event.target.value)}
              rows={5}
              className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal"
            />
          </label>
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.adminFaqAnswerEn}
            <textarea
              value={form.answerEn}
              onChange={(event) => update("answerEn", event.target.value)}
              rows={5}
              dir="ltr"
              className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal"
            />
          </label>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.adminFaqCategory} value={form.category ?? ""} onChange={(event) => update("category", event.target.value || null)} />
            <TextField
              label={messages.adminFaqSort}
              value={String(form.sortOrder)}
              onChange={(event) => update("sortOrder", Number(event.target.value) || 0)}
              inputMode="numeric"
              dir="ltr"
            />
          </div>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.adminSaveChanges}</PrimaryButton>
            {editing ? (
              <SecondaryButton type="button" onClick={() => void toggle(editing)}>
                {editing.status === "Published" ? messages.adminUnpublish : messages.adminPublish}
              </SecondaryButton>
            ) : null}
          </div>
        </div>
      </SideDrawer>
    </main>
  );
}
