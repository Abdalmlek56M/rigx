"use client";

import { useEffect, useMemo, useState } from "react";
import {
  createPolicyVersion,
  getAdminPolicies,
  publishPolicy,
  reviewPolicy,
  updatePolicy,
  type ContentPolicy,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

const kinds = ["Shipping", "Returns", "Exchanges", "Warranty", "Terms"] as const;

export function ContentPoliciesAdmin() {
  const { messages } = useLocale();
  const [policies, setPolicies] = useState<ContentPolicy[]>([]);
  const [kind, setKind] = useState<string>("Shipping");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<ContentPolicy | null>(null);
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ titleAr: "", titleEn: "", bodyAr: "", bodyEn: "" });
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setPolicies(await getAdminPolicies(kind));
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [kind]);

  const statusLabel = useMemo<Record<string, string>>(() => ({
    Draft: messages.contentStatusDraft,
    Review: messages.contentStatusReview,
    Published: messages.contentStatusPublished,
    Archived: messages.contentStatusArchived,
  }), [messages]);

  function openEditor(policy: ContentPolicy) {
    setEditing(policy);
    setForm({ titleAr: policy.titleAr, titleEn: policy.titleEn, bodyAr: policy.bodyAr, bodyEn: policy.bodyEn });
    setNotice(null);
    setError(null);
    setOpen(true);
  }

  async function createVersion() {
    setError(null);
    try {
      const created = await createPolicyVersion(kind);
      setNotice(messages.adminSaved);
      await load();
      openEditor(created);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  async function save() {
    if (!editing) return;
    setSaving(true);
    setError(null);
    try {
      const updated = await updatePolicy(editing.id, { ...form, rowVersion: editing.rowVersion });
      setEditing(updated);
      setNotice(messages.adminSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function advance(policy: ContentPolicy, action: "review" | "publish") {
    setError(null);
    try {
      if (action === "review") await reviewPolicy(policy.id, policy.rowVersion);
      else await publishPolicy(policy.id, policy.rowVersion);
      setNotice(action === "review" ? messages.adminSaved : messages.adminPublished);
      setOpen(false);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-5xl gap-6 px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow="RIGX / CONTENT" title={messages.adminPoliciesTitle} lead={messages.adminPoliciesLead} />
        <PrimaryButton type="button" onClick={() => void createVersion()}>{messages.adminPolicyNewVersion}</PrimaryButton>
      </div>
      <div className="flex flex-wrap gap-2">
        {kinds.map((item) => (
          <button
            key={item}
            type="button"
            onClick={() => setKind(item)}
            aria-pressed={kind === item}
            className={`rounded-full border px-3 py-1.5 text-sm font-semibold ${
              kind === item
                ? "border-[var(--ds-accent)] bg-[var(--ds-accent-soft)] text-[var(--ds-accent-strong)]"
                : "border-[var(--ds-border)] bg-[var(--ds-surface)] text-[var(--ds-text)]"
            }`}
          >
            {item}
          </button>
        ))}
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : policies.length === 0 ? (
        <Message tone="info">{messages.adminNoPolicies}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {policies.map((policy) => (
            <article key={policy.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div className="min-w-0">
                <p className="font-bold text-[var(--ds-text)]">
                  {policy.titleAr} · {messages.adminPolicyVersion} {policy.versionNumber}
                </p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{policy.titleEn}</p>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {statusLabel[policy.status] ?? policy.status}
                </span>
                <SecondaryButton type="button" onClick={() => openEditor(policy)}>{messages.adminEdit}</SecondaryButton>
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer open={open} title={editing ? `${editing.kind} v${editing.versionNumber}` : ""} closeLabel={messages.adminCancel} onClose={() => setOpen(false)}>
        {editing ? (
          <div className="grid gap-4">
            <Message tone="info">{messages.adminPoliciesLead}</Message>
            <TextField label={messages.adminArticleTitleAr} value={form.titleAr} onChange={(event) => setForm((current) => ({ ...current, titleAr: event.target.value }))} />
            <TextField label={messages.adminArticleTitleEn} value={form.titleEn} onChange={(event) => setForm((current) => ({ ...current, titleEn: event.target.value }))} dir="ltr" />
            <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
              {messages.adminPolicyBodyAr}
              <textarea
                value={form.bodyAr}
                onChange={(event) => setForm((current) => ({ ...current, bodyAr: event.target.value }))}
                rows={10}
                className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal"
              />
            </label>
            <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
              {messages.adminPolicyBodyEn}
              <textarea
                value={form.bodyEn}
                onChange={(event) => setForm((current) => ({ ...current, bodyEn: event.target.value }))}
                rows={10}
                dir="ltr"
                className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal"
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.adminSaveChanges}</PrimaryButton>
              {editing.status === "Draft" ? (
                <SecondaryButton type="button" onClick={() => void advance(editing, "review")}>{messages.adminPolicyReview}</SecondaryButton>
              ) : null}
              {editing.status === "Draft" || editing.status === "Review" ? (
                <SecondaryButton type="button" onClick={() => void advance(editing, "publish")}>{messages.adminPublish}</SecondaryButton>
              ) : null}
            </div>
          </div>
        ) : null}
      </SideDrawer>
    </main>
  );
}
