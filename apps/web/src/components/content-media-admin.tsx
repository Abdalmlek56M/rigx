"use client";

import { useEffect, useRef, useState } from "react";
import {
  catalogMediaUrl,
  deleteContentMedia,
  getContentMedia,
  getContentMediaUsages,
  replaceContentMedia,
  updateContentMediaMetadata,
  uploadContentMedia,
  type ContentMediaAsset,
  type ContentMediaUsage,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { ConfirmDialog, LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

export function ContentMediaAdmin() {
  const { messages } = useLocale();
  const [assets, setAssets] = useState<ContentMediaAsset[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [uploadOpen, setUploadOpen] = useState(false);
  const [editing, setEditing] = useState<ContentMediaAsset | null>(null);
  const [usages, setUsages] = useState<ContentMediaUsage[]>([]);
  const [deleting, setDeleting] = useState<ContentMediaAsset | null>(null);
  const [pending, setPending] = useState(false);
  const [uploadForm, setUploadForm] = useState({
    titleAr: "",
    titleEn: "",
    altAr: "",
    altEn: "",
    captionAr: "",
    captionEn: "",
    tags: "",
  });
  const fileRef = useRef<HTMLInputElement>(null);

  async function load(term = search) {
    setLoading(true);
    setError(null);
    try {
      const page = await getContentMedia(term || undefined);
      setAssets(page.items);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load("");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function upload() {
    const file = fileRef.current?.files?.[0];
    if (!file) return;
    setPending(true);
    setError(null);
    try {
      const form = new FormData();
      form.set("image", file);
      form.set("titleAr", uploadForm.titleAr || uploadForm.titleEn);
      form.set("titleEn", uploadForm.titleEn || uploadForm.titleAr);
      form.set("altAr", uploadForm.altAr || uploadForm.altEn);
      form.set("altEn", uploadForm.altEn || uploadForm.altAr);
      form.set("captionAr", uploadForm.captionAr);
      form.set("captionEn", uploadForm.captionEn);
      form.set("tags", uploadForm.tags);
      await uploadContentMedia(form);
      setNotice(messages.adminSaved);
      setUploadOpen(false);
      if (fileRef.current) fileRef.current.value = "";
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  async function openEditor(asset: ContentMediaAsset) {
    setEditing(asset);
    setNotice(null);
    setError(null);
    setUsages([]);
    try {
      setUsages(await getContentMediaUsages(asset.id));
    } catch {
      setUsages([]);
    }
  }

  async function saveMetadata() {
    if (!editing) return;
    setPending(true);
    setError(null);
    try {
      await updateContentMediaMetadata(editing.id, {
        titleAr: editing.titleAr,
        titleEn: editing.titleEn,
        altAr: editing.altAr,
        altEn: editing.altEn,
        captionAr: editing.captionAr,
        captionEn: editing.captionEn,
        tags: editing.tags.join(","),
        rowVersion: editing.rowVersion,
      });
      setNotice(messages.adminSaved);
      setEditing(null);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  async function replaceImage(asset: ContentMediaAsset, file: File | undefined) {
    if (!file) return;
    setPending(true);
    setError(null);
    try {
      const form = new FormData();
      form.set("image", file);
      await replaceContentMedia(asset.id, form, asset.rowVersion);
      setNotice(messages.adminSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  async function confirmDelete() {
    if (!deleting) return;
    setPending(true);
    setError(null);
    try {
      await deleteContentMedia(deleting.id, deleting.rowVersion);
      setDeleting(null);
      setNotice(messages.adminSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-6xl gap-6 px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow="RIGX / MEDIA" title={messages.adminMediaTitle} lead={messages.adminMediaLead} />
        <PrimaryButton type="button" onClick={() => setUploadOpen(true)}>{messages.adminMediaUpload}</PrimaryButton>
      </div>
      <form
        className="flex flex-wrap gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          void load(search);
        }}
      >
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={messages.adminMediaSearch}
          className="min-h-11 flex-1 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
        />
        <SecondaryButton type="submit">{messages.adminMediaSearch}</SecondaryButton>
      </form>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : assets.length === 0 ? (
        <Message tone="info">{messages.adminMediaEmpty}</Message>
      ) : (
        <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {assets.map((asset) => (
            <article key={asset.id} className="rigx-admin-card overflow-hidden">
              <img
                src={catalogMediaUrl(asset.storageKey)}
                alt={asset.altEn}
                className="aspect-[4/3] w-full object-cover"
                loading="lazy"
              />
              <div className="grid gap-1 p-4">
                <p className="font-bold text-[var(--ds-text)]">{asset.titleAr}</p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{asset.titleEn}</p>
                <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">
                  {asset.width}×{asset.height} · {(asset.fileSizeBytes / 1024).toFixed(0)} KB
                </p>
                <div className="mt-2 flex flex-wrap gap-2">
                  <SecondaryButton type="button" onClick={() => void openEditor(asset)}>{messages.adminEdit}</SecondaryButton>
                  <label className="rigx-button rigx-button-secondary cursor-pointer">
                    {messages.adminMediaReplace}
                    <input
                      type="file"
                      accept="image/*"
                      className="sr-only"
                      onChange={(event) => void replaceImage(asset, event.target.files?.[0])}
                    />
                  </label>
                  <SecondaryButton type="button" onClick={() => setDeleting(asset)}>{messages.adminDelete}</SecondaryButton>
                </div>
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer open={uploadOpen} title={messages.adminMediaUpload} closeLabel={messages.adminCancel} onClose={() => setUploadOpen(false)}>
        <div className="grid gap-4">
          <input ref={fileRef} type="file" accept="image/*" className="min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-2 text-sm" />
          <TextField label={messages.adminMediaTitleAr} value={uploadForm.titleAr} onChange={(event) => setUploadForm((current) => ({ ...current, titleAr: event.target.value }))} />
          <TextField label={messages.adminMediaTitleEn} value={uploadForm.titleEn} onChange={(event) => setUploadForm((current) => ({ ...current, titleEn: event.target.value }))} dir="ltr" />
          <TextField label={messages.adminMediaAltAr} value={uploadForm.altAr} onChange={(event) => setUploadForm((current) => ({ ...current, altAr: event.target.value }))} />
          <TextField label={messages.adminMediaAltEn} value={uploadForm.altEn} onChange={(event) => setUploadForm((current) => ({ ...current, altEn: event.target.value }))} dir="ltr" />
          <TextField label={messages.adminMediaCaptionAr} value={uploadForm.captionAr} onChange={(event) => setUploadForm((current) => ({ ...current, captionAr: event.target.value }))} />
          <TextField label={messages.adminMediaCaptionEn} value={uploadForm.captionEn} onChange={(event) => setUploadForm((current) => ({ ...current, captionEn: event.target.value }))} dir="ltr" />
          <TextField label={messages.adminMediaTags} value={uploadForm.tags} onChange={(event) => setUploadForm((current) => ({ ...current, tags: event.target.value }))} />
          <PrimaryButton type="button" onClick={() => void upload()} pending={pending}>{messages.adminMediaUpload}</PrimaryButton>
        </div>
      </SideDrawer>

      <SideDrawer open={editing !== null} title={editing?.titleAr ?? ""} closeLabel={messages.adminCancel} onClose={() => setEditing(null)}>
        {editing ? (
          <div className="grid gap-4">
            <img src={catalogMediaUrl(editing.storageKey)} alt={editing.altEn} className="w-full rounded-xl border border-[var(--ds-border)]" />
            <TextField label={messages.adminMediaTitleAr} value={editing.titleAr} onChange={(event) => setEditing({ ...editing, titleAr: event.target.value })} />
            <TextField label={messages.adminMediaTitleEn} value={editing.titleEn} onChange={(event) => setEditing({ ...editing, titleEn: event.target.value })} dir="ltr" />
            <TextField label={messages.adminMediaAltAr} value={editing.altAr} onChange={(event) => setEditing({ ...editing, altAr: event.target.value })} />
            <TextField label={messages.adminMediaAltEn} value={editing.altEn} onChange={(event) => setEditing({ ...editing, altEn: event.target.value })} dir="ltr" />
            <TextField label={messages.adminMediaCaptionAr} value={editing.captionAr ?? ""} onChange={(event) => setEditing({ ...editing, captionAr: event.target.value })} />
            <TextField label={messages.adminMediaCaptionEn} value={editing.captionEn ?? ""} onChange={(event) => setEditing({ ...editing, captionEn: event.target.value })} dir="ltr" />
            <TextField label={messages.adminMediaTags} value={editing.tags.join(",")} onChange={(event) => setEditing({ ...editing, tags: event.target.value.split(",").map((tag) => tag.trim()).filter(Boolean) })} />
            <div className="grid gap-1">
              <p className="text-sm font-bold text-[var(--ds-text)]">{messages.adminMediaUsages}</p>
              {usages.length === 0 ? <p className="text-xs text-[var(--ds-text-muted)]">{messages.adminMediaEmpty}</p> : (
                <ul className="grid gap-1 text-xs text-[var(--ds-text-muted)]">
                  {usages.map((usage) => (
                    <li key={`${usage.referenceType}-${usage.referenceId}`} className="flex justify-between gap-2" dir="ltr">
                      <span>{usage.referenceType}: {usage.referenceTitle}</span>
                      <span>{usage.isPublished ? messages.contentStatusPublished : messages.contentStatusDraft}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <PrimaryButton type="button" onClick={() => void saveMetadata()} pending={pending}>{messages.adminSaveChanges}</PrimaryButton>
          </div>
        ) : null}
      </SideDrawer>

      <ConfirmDialog
        open={deleting !== null}
        title={messages.adminDelete}
        description={messages.adminMediaDeleteConfirm}
        confirmLabel={messages.adminDelete}
        cancelLabel={messages.adminCancel}
        pending={pending}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setDeleting(null)}
      />
    </main>
  );
}
