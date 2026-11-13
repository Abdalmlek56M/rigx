"use client";

import { useEffect, useState } from "react";
import { getPublicVideo, type ContentVideo } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro } from "@/components/ui";

export function VideoPlayer({ id }: { id: string }) {
  const { messages, locale } = useLocale();
  const [video, setVideo] = useState<ContentVideo | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void getPublicVideo(id).then(setVideo).catch(() => setError(messages.videoUnavailable));
  }, [id, messages]);

  if (!video && !error) return <LoadingCard />;
  if (error) return <div className="order-empty compact-empty"><div className="empty-icon">+</div><h3>{error}</h3></div>;
  if (!video) return null;

  return (
    <>
      <div className="page-hero compact-hero"><div><span className="eyebrow">RIGX / VIDEO</span><h1>{locale === "ar" ? video.titleAr : video.titleEn}</h1><p>{video.descriptionAr ?? video.descriptionEn ?? ""}</p></div><div className="builder-status"><span className="pulse" /> {video.provider ?? "RIGX"}</div></div>
      <div className="video-panel glass-panel">
      <PageIntro eyebrow="RIGX / VIDEO" title={locale === "ar" ? video.titleAr : video.titleEn} lead={video.descriptionAr ?? video.descriptionEn ?? ""} />
      {video.embedUrl ? (
        <div className="mt-6 grid gap-3">
          <div className="aspect-video overflow-hidden rounded-2xl border border-[var(--ds-border)] bg-black">
            <iframe
              src={video.embedUrl}
              title={locale === "ar" ? video.titleAr : video.titleEn}
              loading="lazy"
              allow="encrypted-media; picture-in-picture; fullscreen"
              referrerPolicy="strict-origin-when-cross-origin"
              allowFullScreen
              className="h-full w-full"
            />
          </div>
          <a href={`https://www.youtube.com/watch?v=${video.youtubeVideoId}`} target="_blank" rel="noreferrer" className="text-sm font-bold text-[var(--accent)]">
            {messages.videoWatchOnYouTube}
          </a>
        </div>
      ) : (
        <video
          controls
          preload="metadata"
          playsInline
          className="mt-6 w-full rounded-2xl border border-[var(--ds-border)] bg-black"
          src={`/api/v1/content/videos/${video.id}/stream`}
        />
      )}
      <p className="mt-3 text-xs text-[var(--ds-text-muted)]">
        {video.provider === "YouTube" ? messages.videoExternalNote : messages.videoStorageNote}
      </p>
      </div>
    </>
  );
}

