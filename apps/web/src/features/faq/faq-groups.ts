import type { FaqEntry } from "@/components/ui/FaqList";

export interface FaqSource {
  id: string;
  questionAr: string;
  questionEn: string;
  answerAr: string;
  answerEn: string;
  topic: string | null;
  scope: string;
  youTubeEmbedUrl: string | null;
}

export interface FaqGroup {
  /** `null` for the questions without a topic. */
  topic: string | null;
  entries: FaqEntry[];
}

const YOUTUBE_HOSTS = new Set(["www.youtube.com", "youtube.com", "www.youtube-nocookie.com"]);

/** The privacy-friendly embed address of a YouTube embed link; `null` for anything else (no other site is ever framed). */
export function privacyEmbedUrl(url: string | null): string | null {
  if (!url) return null;
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== "https:" || !YOUTUBE_HOSTS.has(parsed.hostname) || !parsed.pathname.startsWith("/embed/")) return null;
    return `https://www.youtube-nocookie.com${parsed.pathname}${parsed.search}`;
  } catch {
    return null;
  }
}

/** The help page's FAQs (the general ones, in the given order) grouped by topic, in the visitor's language; topic-less ones form the last group. */
export function groupFaqs(faqs: readonly FaqSource[], english: boolean): FaqGroup[] {
  const groups = new Map<string | null, FaqEntry[]>();
  for (const faq of faqs.filter((item) => item.scope === "General")) {
    const topic = faq.topic?.trim() || null;
    const embedUrl = privacyEmbedUrl(faq.youTubeEmbedUrl);
    const entry: FaqEntry = {
      id: faq.id,
      question: english && faq.questionEn ? faq.questionEn : faq.questionAr,
      answer: english && faq.answerEn ? faq.answerEn : faq.answerAr,
      ...(embedUrl ? { embedUrl } : {})
    };
    groups.set(topic, [...(groups.get(topic) ?? []), entry]);
  }
  const named = [...groups].filter(([topic]) => topic !== null).map(([topic, entries]) => ({ topic, entries }));
  const unnamed = groups.get(null);
  return unnamed ? [...named, { topic: null, entries: unnamed }] : named;
}
