/** Article display status: a published article with a future date reads as scheduled; labels live under `admin.articles`. */
export type ArticleStatusKey = "Draft" | "Published" | "Scheduled" | "Archived" | "Other";

export const ARTICLE_FILTERS = ["", "Draft", "Published", "Scheduled"] as const;

export function articleStatusKey(status: string, publishedAtUtc: string | null, now = new Date()): ArticleStatusKey {
  if (status === "Published") {
    if (publishedAtUtc !== null && new Date(publishedAtUtc).getTime() > now.getTime()) return "Scheduled";
    return "Published";
  }
  if (status === "Draft" || status === "Archived") return status;
  return "Other";
}

/** The API status filter a display status maps to (Scheduled is its own API status; Other is not filterable). */
export function articleFilterStatus(status: string): string {
  return ARTICLE_FILTERS.includes(status as (typeof ARTICLE_FILTERS)[number]) ? status : "";
}

export type ArticleStatusTone = "draft" | "live" | "scheduled" | "archived" | "other";

export function articleStatusTone(key: ArticleStatusKey): ArticleStatusTone {
  switch (key) {
    case "Published": return "live";
    case "Scheduled": return "scheduled";
    case "Draft": return "draft";
    case "Archived": return "archived";
    default: return "other";
  }
}
