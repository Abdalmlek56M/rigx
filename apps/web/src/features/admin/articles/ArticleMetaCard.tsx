"use client";

import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { flattenTree } from "../categories/category-rules";
import type { Category } from "../categories/categories-api";
import type { ArticleForm } from "./article-rules";

/** Category, tags, cover image, SEO and order — the right-hand column of the editor. */
export function ArticleMetaCard({ form, set, categories, errors }: {
  form: ArticleForm;
  set: (patch: Partial<ArticleForm>) => void;
  categories: readonly Category[];
  errors: Record<string, string>;
}) {
  const t = useTranslations("admin.articles.meta");
  const categoryOptions = flattenTree(categories, new Set()).map(({ node, depth }) => ({
    value: node.slug,
    label: `${"— ".repeat(depth)}${node.name}`
  }));

  return (
    <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <SelectField
        label={t("category")} name="categorySlug" value={form.categorySlug} onChange={(categorySlug) => set({ categorySlug })}
        options={[{ value: "", label: t("noCategory") }, ...categoryOptions]} error={errors.categorySlug}
      />
      <TextField label={t("tags")} name="tags" value={form.tags} onChange={(tags) => set({ tags })} error={errors.tags} placeholder={t("tagsPlaceholder")} />
      <TextField label={t("image")} name="mainImage" value={form.mainImageKey} onChange={(mainImageKey) => set({ mainImageKey })} ltr error={errors.mainImageStorageKey} />
      {form.mainImageKey ? (
        // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
        <img src={mediaUrl(form.mainImageKey)} alt="" className="h-24 w-full rounded-control border border-line object-cover" />
      ) : null}
      <TextField label={t("sortOrder")} name="sortOrder" type="number" ltr value={form.sortOrder} onChange={(sortOrder) => set({ sortOrder })} error={errors.sortOrder} />

      <div className="flex flex-col gap-3 border-t border-line pt-3">
        <h3 className="m-0 text-sm font-bold">{t("seo")}</h3>
        <TextField label={t("seoTitleAr")} name="seoTitleAr" value={form.seoTitleAr} onChange={(seoTitleAr) => set({ seoTitleAr })} error={errors.seoTitleAr} />
        <TextField label={t("seoTitleEn")} name="seoTitleEn" value={form.seoTitleEn} onChange={(seoTitleEn) => set({ seoTitleEn })} ltr error={errors.seoTitleEn} />
        <TextAreaField label={t("seoDescriptionAr")} name="seoDescriptionAr" value={form.seoDescriptionAr} onChange={(seoDescriptionAr) => set({ seoDescriptionAr })} rows={2} error={errors.seoDescriptionAr} />
        <TextAreaField label={t("seoDescriptionEn")} name="seoDescriptionEn" value={form.seoDescriptionEn} onChange={(seoDescriptionEn) => set({ seoDescriptionEn })} rows={2} error={errors.seoDescriptionEn} />
        <TextField label={t("canonical")} name="canonicalUrl" value={form.canonicalUrl} onChange={(canonicalUrl) => set({ canonicalUrl })} ltr error={errors.canonicalUrl} />
      </div>
    </section>
  );
}
