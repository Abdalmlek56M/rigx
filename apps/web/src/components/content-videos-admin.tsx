"use client";

import { useEffect, useRef, useState } from "react";
import { getAdminVideos, createYouTubeVideo, setContentVideoPublished, uploadContentVideo, type ContentVideo } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, TextField } from "@/components/ui";

export function ContentVideosAdmin() {
  const { messages } = useLocale();
  const [videos, setVideos] = useState<ContentVideo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [form, setForm] = useState({ titleAr: "", titleEn: "", descriptionAr: "", descriptionEn: "" });
  const [youtubeUrl, setYoutubeUrl] = useState("");
  const fileRef = useRef<HTMLInputElement>(null);

  async function load() {
    setLoading(true);
    try { setVideos(await getAdminVideos()); }
    catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setLoading(false); }
  }

  useEffect(() => { void load(); /* eslint-disable-next-line */ }, []);

  async function upload() {
    const file = fileRef.current?.files?.[0];
    if (!file) return;
    setPending(true); setError(null);
    try {
      const body = new FormData();
      body.set("video", file);
      body.set("titleAr", form.titleAr || form.titleEn);
      body.set("titleEn", form.titleEn || form.titleAr);
      body.set("descriptionAr", form.descriptionAr);
      body.set("descriptionEn", form.descriptionEn);
      await uploadContentVideo(body);
      setNotice(messages.adminSaved);
      setForm({ titleAr: "", titleEn: "", descriptionAr: "", descriptionEn: "" });
      if (fileRef.current) fileRef.current.value = "";
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setPending(false); }
  }

  async function addYouTube() {
    if (!form.titleAr.trim() && !form.titleEn.trim()) return;
    setPending(true); setError(null);
    try {
      await createYouTubeVideo({
        titleAr: form.titleAr || form.titleEn,
        titleEn: form.titleEn || form.titleAr,
        descriptionAr: form.descriptionAr || null,
        descriptionEn: form.descriptionEn || null,
        url: youtubeUrl,
      });
      setNotice(messages.adminSaved);
      setYoutubeUrl("");
      setForm({ titleAr: "", titleEn: "", descriptionAr: "", descriptionEn: "" });
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
    finally { setPending(false); }
  }

  async function toggle(video: ContentVideo) {
    try {
      await setContentVideoPublished(video.id, video.status !== "Published", video.rowVersion);
      setNotice(video.status === "Published" ? messages.adminUnpublished : messages.adminPublished);
      await load();
    } catch (requestError) { setError(apiErrorMessage(requestError, messages)); }
  }

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-5xl gap-6 px-4 py-8 sm:px-6">
      <PageIntro eyebrow="RIGX / MEDIA" title={messages.adminVideosTitle} lead={messages.adminVideosLead} />
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
      <section className="rigx-admin-card p-5">
        <h2 className="text-lg font-bold">{messages.adminVideoUpload}</h2>
        <div className="mt-3 grid gap-3">
          <input ref={fileRef} type="file" accept="video/mp4,video/webm" className="min-h-11 rounded-xl border border-[var(--ds-border)] p-2 text-sm" />
          <TextField label={messages.adminMediaTitleAr} value={form.titleAr} onChange={(event) => setForm((current) => ({ ...current, titleAr: event.target.value }))} />
          <TextField label={messages.adminMediaTitleEn} value={form.titleEn} onChange={(event) => setForm((current) => ({ ...current, titleEn: event.target.value }))} dir="ltr" />
          <TextField label={messages.adminArticleSummaryAr} value={form.descriptionAr} onChange={(event) => setForm((current) => ({ ...current, descriptionAr: event.target.value }))} />
          <TextField label={messages.adminArticleSummaryEn} value={form.descriptionEn} onChange={(event) => setForm((current) => ({ ...current, descriptionEn: event.target.value }))} dir="ltr" />
          <PrimaryButton type="button" onClick={() => void upload()} pending={pending}>{messages.adminVideoUpload}</PrimaryButton>
        </div>
      </section>
      <section className="rigx-admin-card p-5">
        <h2 className="text-lg font-bold">{messages.adminVideoYouTube}</h2>
        <p className="mt-1 text-sm text-[var(--ds-text-muted)]">{messages.adminVideoYouTubeHint}</p>
        <div className="mt-3 grid gap-3">
          <TextField label={messages.adminVideoYouTubeUrl} value={youtubeUrl} onChange={(event) => setYoutubeUrl(event.target.value)} dir="ltr" placeholder="https://www.youtube.com/watch?v=..." />
          <PrimaryButton type="button" onClick={() => void addYouTube()} pending={pending}>{messages.adminVideoYouTube}</PrimaryButton>
        </div>
      </section>
      {loading ? <LoadingCard /> : videos.length === 0 ? <Message tone="info">{messages.adminVideosEmpty}</Message> : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {videos.map((video) => (
            <article key={video.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div>
                <p className="font-bold">{video.titleAr}</p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{video.titleEn} · {video.provider} · {video.mimeType} · {(video.fileSizeBytes / 1048576).toFixed(1)} MB</p>
              </div>
              <div className="flex items-center gap-2">
                <span className="rounded-full border border-[var(--ds-border)] px-2.5 py-1 text-xs font-bold">{video.status}</span>
                <SecondaryButton type="button" onClick={() => void toggle(video)}>{video.status === "Published" ? messages.adminUnpublish : messages.adminPublish}</SecondaryButton>
              </div>
            </article>
          ))}
        </section>
      )}
    </main>
  );
}
