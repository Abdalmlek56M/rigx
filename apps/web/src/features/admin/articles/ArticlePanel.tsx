"use client";

import { useCallback } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import type { Category } from "../categories/categories-api";
import { ArticleEditor } from "./ArticleEditor";
import { fetchArticle, fetchRevisions, type Article } from "./articles-api";

/** Loads one article and its revisions, then shows the editor. */
export function ArticlePanel({ id, categories, onChanged }: {
  id: string;
  categories: readonly Category[];
  onChanged: (article?: Article) => void;
}) {
  const t = useTranslations("admin.articles");
  const load = useCallback(
    () => Promise.all([fetchArticle(id), fetchRevisions(id)]).then(([detail, revisions]) => ({ detail, revisions })),
    [id]
  );
  const { state, reload } = useLoad(load);
  if (state.status === "loading") return <div aria-hidden="true" className="h-72 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
        <p className="m-0 font-bold">{t("failed")}</p>
        <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("retry")}</button>
      </div>
    );
  }
  const saved = (article: Article) => { reload(); onChanged(article); };
  return (
    <ArticleEditor
      key={`${state.data.detail.article.id}-${state.data.detail.article.rowVersion}`}
      detail={state.data.detail} revisions={state.data.revisions} categories={categories}
      onSaved={saved} onDeleted={() => onChanged()} onStale={reload} onCancel={() => onChanged()}
    />
  );
}
