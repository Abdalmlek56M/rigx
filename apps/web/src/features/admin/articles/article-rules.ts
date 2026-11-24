/** Article form ⇄ API. Pure, so it is tested. Messages live under `admin.articles`. */
import type { Article, ArticleInput } from "./articles-api";
import { blockRows, blockRowsInput, type BlockRow } from "./article-blocks.ts";

export interface ArticleForm {
  slug: string;
  titleAr: string;
  titleEn: string;
  summaryAr: string;
  summaryEn: string;
  categorySlug: string;
  tags: string;
  mainImageKey: string;
  seoTitleAr: string;
  seoTitleEn: string;
  seoDescriptionAr: string;
  seoDescriptionEn: string;
  canonicalUrl: string;
  sortOrder: string;
  blocksAr: BlockRow[];
  blocksEn: BlockRow[];
}

/** The recommended lengths of what a search result shows: the title and the description (counted in the SEO fields; the save never refuses them). */
export const SEO_TITLE_LIMIT = 60;
export const SEO_DESCRIPTION_LIMIT = 160;

/**
 * The address part made from an English title: lowercase letters and digits joined by single hyphens (the server's slug rule), accents
 * dropped, anything else (spaces, punctuation, Arabic letters) a hyphen. An empty result means the title has nothing a slug can use.
 */
export function slugify(title: string): string {
  return title
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80)
    .replace(/-+$/g, "");
}

export function emptyArticleForm(): ArticleForm {
  return {
    slug: "", titleAr: "", titleEn: "", summaryAr: "", summaryEn: "", categorySlug: "", tags: "", mainImageKey: "",
    seoTitleAr: "", seoTitleEn: "", seoDescriptionAr: "", seoDescriptionEn: "", canonicalUrl: "", sortOrder: "0",
    blocksAr: [], blocksEn: []
  };
}

export function articleForm(article: Article): ArticleForm {
  return {
    slug: article.slug,
    titleAr: article.titleAr,
    titleEn: article.titleEn,
    summaryAr: article.summaryAr,
    summaryEn: article.summaryEn,
    categorySlug: article.categorySlug ?? "",
    tags: article.tags.join(", "),
    mainImageKey: article.mainImageStorageKey ?? "",
    seoTitleAr: article.seoTitleAr ?? "",
    seoTitleEn: article.seoTitleEn ?? "",
    seoDescriptionAr: article.seoDescriptionAr ?? "",
    seoDescriptionEn: article.seoDescriptionEn ?? "",
    canonicalUrl: article.canonicalUrl ?? "",
    sortOrder: String(article.sortOrder),
    blocksAr: blockRows(article.blocksAr),
    blocksEn: blockRows(article.blocksEn)
  };
}

export function articleValid(form: ArticleForm): boolean {
  return form.slug.trim() !== "" && form.titleAr.trim() !== "" && form.titleEn.trim() !== "";
}

export function toArticleInput(form: ArticleForm, rowVersion: number): ArticleInput {
  const text = (value: string) => value.trim() === "" ? null : value.trim();
  const sortOrder = Number.parseInt(form.sortOrder, 10);
  return {
    slug: form.slug.trim(),
    titleAr: form.titleAr.trim(),
    titleEn: form.titleEn.trim(),
    summaryAr: form.summaryAr.trim(),
    summaryEn: form.summaryEn.trim(),
    blocksAr: blockRowsInput(form.blocksAr),
    blocksEn: blockRowsInput(form.blocksEn),
    mainImageStorageKey: text(form.mainImageKey),
    categorySlug: text(form.categorySlug),
    tags: text(form.tags),
    seoTitleAr: text(form.seoTitleAr),
    seoTitleEn: text(form.seoTitleEn),
    seoDescriptionAr: text(form.seoDescriptionAr),
    seoDescriptionEn: text(form.seoDescriptionEn),
    canonicalUrl: text(form.canonicalUrl),
    sortOrder: Number.isFinite(sortOrder) ? sortOrder : 0,
    rowVersion
  };
}
