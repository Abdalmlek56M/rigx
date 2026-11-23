"use client";

import { useCallback } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import type { Category } from "../categories/categories-api";
import { ArticleEditor } from "./ArticleEditor";
import { fetchArticle, fetchRevisions, type Article } from "./articles-api";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";

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
  if (state.status === "loading") return <LoadingCard className="h-72" />;
  if (state.status === "error") {
    return (
      <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
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
