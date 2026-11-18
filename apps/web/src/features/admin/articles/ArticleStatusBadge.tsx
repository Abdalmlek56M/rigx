"use client";

import { useTranslations } from "next-intl";
import { articleStatusKey, articleStatusTone } from "./article-labels";

const TONES = {
  live: "bg-success text-surface",
  scheduled: "bg-orange/15 text-orange-text",
  draft: "bg-line text-ink-muted",
  archived: "bg-line text-ink-muted",
  other: "bg-line text-ink-muted"
} as const;

export function ArticleStatusBadge({ status, publishedAtUtc }: { status: string; publishedAtUtc: string | null }) {
  const t = useTranslations("admin.articles.statuses");
  const key = articleStatusKey(status, publishedAtUtc);
  return <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${TONES[articleStatusTone(key)]}`}>{t(key)}</span>;
}
