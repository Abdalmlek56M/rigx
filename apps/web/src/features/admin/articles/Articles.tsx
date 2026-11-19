"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { flattenTree } from "../categories/category-rules";
import { fetchCategories } from "../categories/categories-api";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useUrlFilters } from "../kit/use-url-filters";
import { useReveal } from "../kit/use-reveal";
import { ArticleEditor } from "./ArticleEditor";
import { ArticlePanel } from "./ArticlePanel";
import { ArticleStatusBadge } from "./ArticleStatusBadge";
import { articleFilterStatus } from "./article-labels";
import { fetchArticles, type Article } from "./articles-api";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const FILTERS = { status: "", q: "", page: "1", id: "" };
const PAGE_SIZE = 20;

/** ADM-Articles: the paged list and the editor for the selected or a new article. */
export function Articles() {
  const t = useTranslations("admin.articles");
  const { values, set } = useUrlFilters(FILTERS);
  const page = pageNumber(values.page);
  const [creating, setCreating] = useState(false);
  const load = useCallback(async () => {
    const [articles, categories] = await Promise.all([
      fetchArticles({
        page, pageSize: PAGE_SIZE, status: articleFilterStatus(values.status) || undefined, search: values.q || undefined
      }),
      fetchCategories().catch(() => [])
    ]);
    return { articles, categories };
  }, [page, values.status, values.q]);
  const { state, reload } = useLoad(load);
  const data = state.status === "ready" ? state.data : { articles: { items: [], page: 1, pageSize: PAGE_SIZE, total: 0 }, categories: [] };
  const names = new Map(flattenTree(data.categories, new Set()).map(({ node }) => [node.slug, node.name]));
  const panelRef = useReveal<HTMLDivElement>(creating ? "new" : values.id || null);
  const tableState: Load<readonly Article[]> = state.status === "ready" ? { status: "ready", data: state.data.articles.items } : state;

  const columns: Column<Article>[] = [
    {
      key: "title", header: t("columns.title"), cell: (article) => (
        <button
          type="button" aria-current={article.id === values.id ? "true" : undefined}
          onClick={() => { setCreating(false); set({ id: article.id }); }}
          className={`flex min-w-0 flex-col items-start gap-0.5 text-start ${article.id === values.id ? "text-orange-text" : ""}`}
        >
          <span dir="auto" className="max-w-72 truncate font-bold [unicode-bidi:isolate]">{article.titleAr}</span>
          <span dir="ltr" className="font-mono text-xs text-ink-muted">{article.slug}</span>
        </button>
      )
    },
    { key: "category", header: t("columns.category"), cell: (article) => <span className="text-sm">{article.categorySlug ? names.get(article.categorySlug) ?? article.categorySlug : t("none")}</span> },
    { key: "status", header: t("columns.status"), cell: (article) => <ArticleStatusBadge status={article.status} publishedAtUtc={article.publishedAtUtc} /> },
    { key: "views", header: t("columns.views"), align: "end", cell: (article) => <span className="text-sm">{article.viewCount}</span> }
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={
          <Button variant="primary" icon={PlusIcon} onClick={() => { setCreating(true); set({ id: "" }); }}>{t("new")}</Button>
        }
      />
      <FilterBar>
        <SearchBox label={t("search")} value={values.q} onChange={(q) => set({ q, id: "" })} />
        <FilterSelect
          label={t("columns.status")} value={values.status} onChange={(status) => set({ status, id: "" })}
          options={[
            { value: "", label: t("statuses.All") },
            { value: "Draft", label: t("statuses.Draft") },
            { value: "Published", label: t("statuses.Published") },
            { value: "Scheduled", label: t("statuses.Scheduled") }
          ]}
        />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(article) => article.id} empty={t("empty")} />
      {state.status === "ready" ? (
        <Pager info={{ page: state.data.articles.page, pageSize: state.data.articles.pageSize, total: state.data.articles.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}

      <div ref={panelRef} className="scroll-mt-20">
        {creating ? (
          <section className="flex flex-col gap-3">
            <h2 className="m-0 text-[17px] font-bold">{t("newTitle")}</h2>
            <ArticleEditor
              key="new" detail={null} revisions={[]} categories={data.categories}
              onSaved={(saved) => { setCreating(false); set({ id: saved.id }); reload(); }}
              onDeleted={() => reload()} onStale={reload} onCancel={() => setCreating(false)}
            />
          </section>
        ) : values.id ? (
          <ArticlePanel id={values.id} categories={data.categories} onChanged={(saved) => { if (!saved) set({ id: "" }); reload(); }} />
        ) : (
          <p className="m-0 text-sm text-ink-muted">{t("pick")}</p>
        )}
      </div>
    </>
  );
}
