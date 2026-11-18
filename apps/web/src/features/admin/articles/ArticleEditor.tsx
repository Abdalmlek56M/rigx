"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import type { Category } from "../categories/categories-api";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { ArticleBlocksEditor } from "./ArticleBlocksEditor";
import { ArticleMetaCard } from "./ArticleMetaCard";
import { ArticlePublishCard } from "./ArticlePublishCard";
import { createArticle, updateArticle, type Article, type ArticleDetail, type ArticleRevision } from "./articles-api";
import { articleForm, articleValid, emptyArticleForm, toArticleInput, type ArticleForm as Form } from "./article-rules";
import { isConflict } from "@/lib/api/conflict";

/** The article editor: bilingual text and blocks on the start side, publishing and metadata on the end side. */
export function ArticleEditor({ detail, revisions, categories, onSaved, onDeleted, onStale, onCancel }: {
  detail: ArticleDetail | null;
  revisions: readonly ArticleRevision[];
  categories: readonly Category[];
  onSaved: (article: Article) => void;
  onDeleted: () => void;
  onStale: () => void;
  onCancel: () => void;
}) {
  const t = useTranslations("admin.articles");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => detail ? articleForm(detail.article) : emptyArticleForm());
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const errors = submit.errors.fields;

  async function save() {
    if (!articleValid(form)) return;
    await submit.run(async () => {
      try {
        onSaved(detail
          ? await updateArticle(detail.article.id, toArticleInput(form, detail.article.rowVersion))
          : await createArticle(toArticleInput(form, 0)));
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <form
      className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_340px] lg:items-start"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <div className="flex min-w-0 flex-col gap-4">
        <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
          <div className="grid gap-3 sm:grid-cols-2">
            <TextField label={t("fields.titleAr")} name="titleAr" dir="rtl" value={form.titleAr} onChange={(titleAr) => set({ titleAr })} error={errors.titleAr} required />
            <TextField label={t("fields.titleEn")} name="titleEn" value={form.titleEn} onChange={(titleEn) => set({ titleEn })} ltr error={errors.titleEn} required />
            <TextField label={t("fields.slug")} name="slug" value={form.slug} onChange={(slug) => set({ slug })} ltr required error={errors.slug} />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <TextAreaField label={t("fields.summaryAr")} name="summaryAr" dir="rtl" value={form.summaryAr} onChange={(summaryAr) => set({ summaryAr })} rows={3} error={errors.summaryAr} />
            <TextAreaField label={t("fields.summaryEn")} name="summaryEn" dir="ltr" value={form.summaryEn} onChange={(summaryEn) => set({ summaryEn })} rows={3} error={errors.summaryEn} />
          </div>
        </section>

        <section className="flex flex-col gap-5 rounded-[10px] border border-line bg-surface p-5">
          <FormMessage message={errors.blocksAr ?? errors.blocksEn ?? null} />
          <ArticleBlocksEditor title={t("blocksArabic")} dir="rtl" rows={form.blocksAr} onChange={(blocksAr) => set({ blocksAr })} />
          <div className="border-t border-line pt-4">
            <ArticleBlocksEditor title={t("blocksEnglish")} dir="ltr" rows={form.blocksEn} onChange={(blocksEn) => set({ blocksEn })} />
          </div>
        </section>

        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap gap-2">
          <button type="submit" disabled={submit.busy || !articleValid(form)} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
          {detail ? null : <button type="button" onClick={onCancel} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{kit("cancel")}</button>}
        </div>
        {submit.dialog}
      </div>

      <div className="flex min-w-0 flex-col gap-4">
        {detail ? <ArticlePublishCard detail={detail} revisions={revisions} onSaved={onSaved} onDeleted={onDeleted} onStale={onStale} /> : null}
        <ArticleMetaCard form={form} set={set} categories={categories} errors={errors} />
      </div>
    </form>
  );
}
