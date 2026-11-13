"use client";

import { useEffect, useState } from "react";
import {
  catalogMediaUrl,
  createArticle,
  getAdminArticle,
  getAdminArticles,
  getArticleRevisions,
  getContentMedia,
  publishArticle,
  unpublishArticle,
  updateArticle,
  type ContentArticle,
  type ContentArticleInput,
  type ContentArticleRevision,
  type ContentMediaAsset,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import {
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  SideDrawer,
  TextField,
} from "@/components/ui";

const emptyArticle: ContentArticleInput = {
  slug: "",
  titleAr: "",
  titleEn: "",
  summaryAr: "",
  summaryEn: "",
  bodyAr: "",
  bodyEn: "",
  mainImageStorageKey: null,
  categorySlug: null,
  tags: "",
  seoTitleAr: null,
  seoTitleEn: null,
  seoDescriptionAr: null,
  seoDescriptionEn: null,
  canonicalUrl: null,
  sortOrder: 0,
};

type FormState = { [K in keyof ContentArticleInput]: ContentArticleInput[K] extends string | null ? string : ContentArticleInput[K] };

function toForm(input: ContentArticleInput | ContentArticle): FormState {
  const tags = Array.isArray(input.tags) ? input.tags.join(",") : input.tags;
  return {
    ...input,
    tags,
    mainImageStorageKey: input.mainImageStorageKey ?? "",
    categorySlug: input.categorySlug ?? "",
    seoTitleAr: input.seoTitleAr ?? "",
    seoTitleEn: input.seoTitleEn ?? "",
    seoDescriptionAr: input.seoDescriptionAr ?? "",
    seoDescriptionEn: input.seoDescriptionEn ?? "",
    canonicalUrl: input.canonicalUrl ?? "",
  };
}

function fromForm(form: FormState, rowVersion?: number): ContentArticleInput {
  return {
    ...form,
    mainImageStorageKey: form.mainImageStorageKey.trim() || null,
    categorySlug: form.categorySlug.trim() || null,
    seoTitleAr: form.seoTitleAr.trim() || null,
    seoTitleEn: form.seoTitleEn.trim() || null,
    seoDescriptionAr: form.seoDescriptionAr.trim() || null,
    seoDescriptionEn: form.seoDescriptionEn.trim() || null,
    canonicalUrl: form.canonicalUrl.trim() || null,
    rowVersion,
  };
}

export function ContentArticlesAdmin() {
  const { messages } = useLocale();
  const [articles, setArticles] = useState<ContentArticle[]>([]);
  const [media, setMedia] = useState<ContentMediaAsset[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<ContentArticle | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [form, setForm] = useState<FormState>(toForm(emptyArticle));
  const [saving, setSaving] = useState(false);
  const [revisions, setRevisions] = useState<ContentArticleRevision[]>([]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [page, mediaPage] = await Promise.all([
        getAdminArticles({ pageSize: 50 }),
        getContentMedia().catch(() => ({ items: [] as ContentMediaAsset[], page: 1, pageSize: 0, total: 0 })),
      ]);
      setArticles(page.items);
      setMedia(mediaPage.items);
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

  function openNew() {
    setEditing(null);
    setForm(toForm(emptyArticle));
    setRevisions([]);
    setNotice(null);
    setError(null);
    setDrawerOpen(true);
  }

  async function openEdit(article: ContentArticle) {
    setNotice(null);
    setError(null);
    setDrawerOpen(true);
    try {
      const [fresh, history] = await Promise.all([
        getAdminArticle(article.id),
        getArticleRevisions(article.id),
      ]);
      setEditing(fresh);
      setForm(toForm(fresh));
      setRevisions(history);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      if (editing) {
        const updated = await updateArticle(editing.id, fromForm(form, editing.rowVersion));
        setEditing(updated);
        setForm(toForm(updated));
        setNotice(messages.adminSaved);
      } else {
        const created = await createArticle(fromForm(form));
        setEditing(created);
        setForm(toForm(created));
        setNotice(messages.adminSaved);
      }
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function togglePublished(article: ContentArticle, published: boolean) {
    setError(null);
    setNotice(null);
    try {
      if (published) await publishArticle(article.id, article.rowVersion);
      else await unpublishArticle(article.id, article.rowVersion);
      setNotice(published ? messages.adminPublished : messages.adminUnpublished);
      setDrawerOpen(false);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  const statusLabel: Record<string, string> = {
    Draft: messages.contentStatusDraft,
    Published: messages.contentStatusPublished,
  };

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-6xl gap-6 px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow="RIGX / CONTENT" title={messages.adminArticlesTitle} lead={messages.adminArticlesLead} />
        <PrimaryButton type="button" onClick={openNew}>{messages.adminArticleNew}</PrimaryButton>
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : articles.length === 0 ? (
        <Message tone="info">{messages.adminNoArticles}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {articles.map((article) => (
            <article key={article.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div className="min-w-0">
                <p className="font-bold text-[var(--ds-text)]">{article.titleAr}</p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{article.slug}</p>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {statusLabel[article.status] ?? article.status}
                </span>
                <SecondaryButton type="button" onClick={() => void openEdit(article)}>{messages.adminEdit}</SecondaryButton>
                <SecondaryButton
                  type="button"
                  onClick={() => void togglePublished(article, article.status !== "Published")}
                >
                  {article.status === "Published" ? messages.adminUnpublish : messages.adminPublish}
                </SecondaryButton>
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer
        open={drawerOpen}
        title={editing ? messages.adminArticleEdit : messages.adminArticleNew}
        closeLabel={messages.adminCancel}
        onClose={() => setDrawerOpen(false)}
      >
        <div className="grid gap-4">
          <TextField label={messages.adminArticleSlug} value={form.slug} onChange={(event) => update("slug", event.target.value)} dir="ltr" required />
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.adminArticleTitleAr} value={form.titleAr} onChange={(event) => update("titleAr", event.target.value)} required />
            <TextField label={messages.adminArticleTitleEn} value={form.titleEn} onChange={(event) => update("titleEn", event.target.value)} dir="ltr" required />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.adminArticleSummaryAr} value={form.summaryAr} onChange={(event) => update("summaryAr", event.target.value)} />
            <TextField label={messages.adminArticleSummaryEn} value={form.summaryEn} onChange={(event) => update("summaryEn", event.target.value)} dir="ltr" />
          </div>
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.adminArticleBodyAr}
            <textarea
              value={form.bodyAr}
              onChange={(event) => update("bodyAr", event.target.value)}
              rows={8}
              className="min-h-40 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal text-[var(--ds-text)]"
            />
          </label>
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.adminArticleBodyEn}
            <textarea
              value={form.bodyEn}
              onChange={(event) => update("bodyEn", event.target.value)}
              rows={8}
              dir="ltr"
              className="min-h-40 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 py-2 text-sm font-normal text-[var(--ds-text)]"
            />
          </label>
          <div className="grid gap-2">
            <span className="text-sm font-semibold text-[var(--ds-text)]">{messages.adminArticleImage}</span>
            {form.mainImageStorageKey ? (
              <img
                src={catalogMediaUrl(form.mainImageStorageKey)}
                alt=""
                className="h-24 w-40 rounded-xl border border-[var(--ds-border)] object-cover"
              />
            ) : null}
            <select
              value={form.mainImageStorageKey}
              onChange={(event) => update("mainImageStorageKey", event.target.value)}
              className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
            >
              <option value="">{messages.adminClearImage}</option>
              {media.map((asset) => (
                <option key={asset.id} value={asset.storageKey}>{asset.titleAr}</option>
              ))}
            </select>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.adminArticleCategory} value={form.categorySlug} onChange={(event) => update("categorySlug", event.target.value)} dir="ltr" />
            <TextField label={messages.adminArticleTags} value={form.tags} onChange={(event) => update("tags", event.target.value)} />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.adminSeoTitleAr} value={form.seoTitleAr} onChange={(event) => update("seoTitleAr", event.target.value)} />
            <TextField label={messages.adminSeoTitleEn} value={form.seoTitleEn} onChange={(event) => update("seoTitleEn", event.target.value)} dir="ltr" />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.adminSeoDescriptionAr} value={form.seoDescriptionAr} onChange={(event) => update("seoDescriptionAr", event.target.value)} />
            <TextField label={messages.adminSeoDescriptionEn} value={form.seoDescriptionEn} onChange={(event) => update("seoDescriptionEn", event.target.value)} dir="ltr" />
          </div>
          <div className="grid gap-4 sm:grid-cols-[1fr_8rem]">
            <TextField label={messages.adminCanonical} value={form.canonicalUrl} onChange={(event) => update("canonicalUrl", event.target.value)} dir="ltr" />
            <TextField
              label={messages.adminSortOrder}
              value={String(form.sortOrder)}
              onChange={(event) => update("sortOrder", Number(event.target.value) || 0)}
              inputMode="numeric"
              dir="ltr"
            />
          </div>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.adminSaveChanges}</PrimaryButton>
            {editing ? (
              <SecondaryButton type="button" onClick={() => void togglePublished(editing, editing.status !== "Published")}>
                {editing.status === "Published" ? messages.adminUnpublish : messages.adminPublish}
              </SecondaryButton>
            ) : null}
          </div>
          {revisions.length > 0 ? (
            <div className="grid gap-1 border-t border-[var(--ds-border)] pt-3">
              <p className="text-sm font-bold text-[var(--ds-text)]">{messages.adminRevisions}</p>
              <ul className="grid gap-1 text-xs text-[var(--ds-text-muted)]">
                {revisions.map((revision) => (
                  <li key={revision.id} dir="ltr">
                    {new Date(revision.createdAtUtc).toLocaleString("en-GB")} · {revision.reason}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>
      </SideDrawer>
    </main>
  );
}
