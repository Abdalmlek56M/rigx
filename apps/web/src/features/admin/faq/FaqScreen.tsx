"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { flattenTree } from "../categories/category-rules";
import { fetchCategories } from "../categories/categories-api";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { RowAction } from "../kit/RowAction";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { useReveal } from "../kit/use-reveal";
import { fetchFaqs, type Faq } from "./faq-api";
import { FaqStatusBadge } from "./FaqActions";
import { FaqForm } from "./FaqForm";
import { FAQ_SCOPES, faqScopeKey } from "./faq-rules";
import { PlusIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button } from "../kit/Button";

const FILTERS = { scope: "", id: "" };

/** ADM-FAQ: the list on the start side, the editor for the selected question on the end side. */
export function FaqScreen() {
  const t = useTranslations("admin.faq");
  const { values, set } = useUrlFilters(FILTERS);
  const [creating, setCreating] = useState(false);
  const load = useCallback(async () => {
    const [faqs, categories] = await Promise.all([
      fetchFaqs({ scope: values.scope || undefined }),
      fetchCategories().catch(() => [])
    ]);
    return { faqs, categories };
  }, [values.scope]);
  const { state, reload } = useLoad(load);
  const data = state.status === "ready" ? state.data : { faqs: [], categories: [] };
  const activeId = creating ? null : values.id || data.faqs[0]?.id || null;
  const selected = data.faqs.find((faq) => faq.id === activeId) ?? null;
  const panelRef = useReveal<HTMLElement>(creating ? "new" : values.id || null);
  const names = new Map(flattenTree(data.categories, new Set()).map(({ node }) => [node.id, node.name]));
  const select = (id: string) => { setCreating(false); set({ id }); };

  function scopeText(faq: Faq): string {
    const key = faqScopeKey(faq.scope);
    if (key === "Category") return names.get(faq.categoryId ?? "") ?? t("scopes.Category");
    if (key === "Product") return faq.itemCode ?? t("scopes.Product");
    return key === "Other" ? t("scopes.Other") : t(`scopes.${key}`);
  }

  const columns: Column<Faq>[] = [
    {
      key: "question",
      header: t("columns.question"),
      cell: (faq) => (
        <button type="button" aria-current={faq.id === activeId ? "true" : undefined} onClick={() => select(faq.id)} className={`flex min-w-0 flex-col items-start gap-0.5 text-start ${faq.id === activeId ? "text-orange-text" : ""}`}>
          <span dir="auto" className="max-w-72 truncate font-bold [unicode-bidi:isolate]">{faq.questionAr}</span>
          {faq.topic ? <span className="text-xs text-ink-muted">{faq.topic}</span> : null}
        </button>
      )
    },
    { key: "scope", header: t("columns.scope"), cell: (faq) => <span className="whitespace-nowrap text-sm">{scopeText(faq)}</span> },
    {
      key: "excluded", header: t("columns.excluded"), align: "end",
      cell: (faq) => <span className="text-xs text-ink-muted">{faq.excludedCategoryIds.length + faq.excludedItemCodes.length || t("none")}</span>
    },
    { key: "status", header: t("columns.status"), cell: (faq) => <FaqStatusBadge status={faq.status} /> },
    actionsColumn((faq) => <RowAction kind="edit" subject={faq.questionAr} current={faq.id === activeId} onClick={() => select(faq.id)} />)
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={
          <Button variant="primary" icon={PlusIcon} onClick={() => { setCreating(true); set({ id: "" }); }}>
            {t("new")}
          </Button>
        }
      />
      <FilterBar>
        <FilterSelect
          label={t("scopeFilter")} value={values.scope} onChange={(scope) => set({ scope, id: "" })}
          options={[{ value: "", label: t("scopes.All") }, ...FAQ_SCOPES.map((scope) => ({ value: scope, label: t(`scopes.${scope}`) }))]}
        />
      </FilterBar>
      {state.status === "error" ? (
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
      ) : (
        <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_420px] lg:items-start">
          {state.status === "loading" ? <LoadingCard className="h-64" /> : (
            <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: data.faqs }} onRetry={reload} rowKey={(faq) => faq.id} empty={t("empty")} />
          )}
          <section ref={panelRef} className={cardClass({ className: "scroll-mt-20 gap-3" })}>
            <h2 className="m-0 text-[17px] font-bold">{creating ? t("newTitle") : selected ? t("editTitle") : t("pick")}</h2>
            {creating ? (
              <FaqForm key="new" faq={null} categories={data.categories} onSaved={(saved) => { setCreating(false); set({ id: saved.id }); reload(); }} onDeleted={() => reload()} onStale={reload} onCancel={() => setCreating(false)} />
            ) : selected ? (
              <FaqForm key={`${selected.id}-${selected.rowVersion}`} faq={selected} categories={data.categories} onSaved={(saved) => { set({ id: saved.id }); reload(); }} onDeleted={() => { set({ id: "" }); reload(); }} onStale={reload} onCancel={() => set({ id: "" })} />
            ) : (
              <p className="m-0 text-sm text-ink-muted">{t("emptyEditor")}</p>
            )}
          </section>
        </div>
      )}
    </>
  );
}
