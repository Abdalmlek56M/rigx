import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Article } from "./articles-api.ts";
import { articleForm, articleValid, emptyArticleForm, toArticleInput } from "./article-rules.ts";

const article: Article = {
  id: "a1", slug: "buying-guide", titleAr: "دليل", titleEn: "Guide", summaryAr: "ملخص", summaryEn: "Summary",
  blocksAr: [{ type: "paragraph", spans: [{ text: "نص", bold: false }] }],
  blocksEn: [{ type: "heading", level: 2, text: "Head" }],
  mainImageStorageKey: "content/a.webp", categorySlug: "guide", tags: ["gpu", "used"], status: "Draft", viewCount: 4,
  seoTitleAr: null, seoTitleEn: "SEO", seoDescriptionAr: null, seoDescriptionEn: null, canonicalUrl: null,
  sortOrder: 2, publishedAtUtc: null, createdAtUtc: "2026-11-01T00:00:00Z", updatedAtUtc: "2026-11-01T00:00:00Z", rowVersion: 5
};

describe("article form", () => {
  it("maps a view to the form and back", () => {
    const form = articleForm(article);
    assert.deepEqual([form.tags, form.categorySlug, form.sortOrder, form.blocksAr.length, form.blocksEn.length], ["gpu, used", "guide", "2", 1, 1]);
    const input = toArticleInput({ ...form, titleAr: " دليل ", canonicalUrl: " /articles/a " }, 5);
    assert.equal(input.titleAr, "دليل");
    assert.equal(input.canonicalUrl, "/articles/a");
    assert.equal(input.tags, "gpu, used");
    assert.equal(input.rowVersion, 5);
    assert.deepEqual(input.blocksAr, [{ type: "paragraph", spans: [{ text: "نص", bold: false }] }]);
    assert.deepEqual(input.blocksEn, [{ type: "heading", level: 2, text: "Head" }]);
  });

  it("turns empty optional fields into null", () => {
    const input = toArticleInput({ ...emptyArticleForm(), slug: "s", titleAr: "ع", titleEn: "E" }, 0);
    assert.deepEqual([input.mainImageStorageKey, input.categorySlug, input.tags, input.seoTitleAr, input.canonicalUrl],
      [null, null, null, null, null]);
    assert.deepEqual(input.blocksAr, []);
  });

  it("needs the slug and both titles", () => {
    assert.equal(articleValid(emptyArticleForm()), false);
    assert.equal(articleValid({ ...emptyArticleForm(), slug: "s", titleAr: "ع", titleEn: "E" }), true);
    assert.equal(articleValid({ ...emptyArticleForm(), slug: "", titleAr: "ع", titleEn: "E" }), false);
  });
});
