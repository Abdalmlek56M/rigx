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
