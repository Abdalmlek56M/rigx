"use client";

import { useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { articleStatusKey, articleStatusTone } from "./article-labels";

const TONES = { live: "success", scheduled: "warning", draft: "neutral", archived: "neutral", other: "neutral" } as const satisfies Record<string, StatusTone>;

export function ArticleStatusBadge({ status, publishedAtUtc }: { status: string; publishedAtUtc: string | null }) {
  const t = useTranslations("admin.articles.statuses");
  const key = articleStatusKey(status, publishedAtUtc);
  return <StatusBadge tone={TONES[articleStatusTone(key)]}>{t(key)}</StatusBadge>;
}
