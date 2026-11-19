"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { PageHeader } from "../kit/PageHeader";
import { useReveal } from "../kit/use-reveal";
import { usePermissions } from "../use-permissions";
import { CategoryForm } from "./CategoryForm";
import { CategoryImageCard } from "./CategoryImageCard";
import { CategorySpecsEditor } from "./CategorySpecsEditor";
import { CategoryTree } from "./CategoryTree";
import { effectiveSpecs, findPath } from "./category-rules";
import { fetchCategories, type Category } from "./categories-api";

/** ADM-Categories: the category tree on the start side and the edit / create panel on the end side. */
export function Categories() {
  const t = useTranslations("admin.categories");
  const { has } = usePermissions();
  const canManage = has("Catalog.ManageTaxonomy");
  const load = useCallback(() => fetchCategories(), []);
  const { state, reload } = useLoad(load);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [creating, setCreating] = useState<{ parentId: string | null } | null>(null);
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(new Set());

  const tree = state.status === "ready" ? state.data : [];
  const activeId = creating ? null : selectedId ?? tree[0]?.id ?? null;
  const selected = findPath(tree, activeId ?? "").at(-1) ?? null;
  const panelRef = useReveal<HTMLElement>(creating ? `new-${creating.parentId ?? "root"}` : selectedId);

  const toggle = (id: string) => setCollapsed((current) => {
    const next = new Set(current);
    if (!next.delete(id)) next.add(id);
    return next;
  });
  const saved = (category: Category) => {
    setCreating(null);
    setSelectedId(category.id);
    reload();
  };

  const button = "inline-flex h-11 items-center whitespace-nowrap rounded-control px-4 text-sm font-bold";

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={canManage ? (
          <button type="button" onClick={() => { setCreating({ parentId: null }); setSelectedId(null); }} className={`${button} bg-carbon text-on-carbon`}>
            {t("newRoot")}
          </button>
        ) : null}
      />
      {state.status === "error" ? (
        <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
          <p className="m-0 font-bold">{t("failed")}</p>
          <button type="button" onClick={reload} className={`${button} bg-carbon text-on-carbon`}>{t("retry")}</button>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[360px_minmax(0,1fr)] lg:items-start">
          <div className="flex flex-col gap-2">
            {state.status === "loading" ? <div aria-hidden="true" className="h-64 animate-pulse rounded-card border border-line bg-surface" /> : (
              <CategoryTree nodes={tree} selectedId={activeId} collapsed={collapsed} onSelect={(id) => { setSelectedId(id); setCreating(null); }} onToggle={toggle} />
            )}
          </div>

          <section ref={panelRef} className="scroll-mt-20 flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
            {creating ? (
              <>
                <h2 className="m-0 text-[17px] font-bold">
                  {creating.parentId ? t("createChildTitle", { parent: findPath(tree, creating.parentId).at(-1)?.name ?? "" }) : t("createTitle")}
                </h2>
                <CategoryForm
                  key={`new-${creating.parentId ?? "root"}`} tree={tree} category={null} parentId={creating.parentId}
                  canManage={canManage} onSaved={saved} onStale={reload} onCancel={() => setCreating(null)}
                />
              </>
            ) : selected ? (
              <>
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <h2 className="m-0 text-[17px] font-bold">
                    {t("editTitle", { name: selected.name })}
                    <span className="ms-2 text-[13px] font-medium text-ink-muted">
                      {selected.parentId ? t("under", { parent: findPath(tree, selected.parentId).at(-1)?.name ?? "" }) : ""}
                    </span>
                  </h2>
                  {canManage ? (
                    <button type="button" onClick={() => setCreating({ parentId: selected.id })} className={`${button} border border-input-line bg-surface`}>
                      {t("newChild")}
                    </button>
                  ) : null}
                </div>
                <CategoryForm key={`edit-${selected.id}`} tree={tree} category={selected} parentId={null} canManage={canManage} onSaved={saved} onStale={reload} onCancel={() => setSelectedId(null)} />
                <CategoryImageCard category={selected} onChanged={() => reload()} />
                <CategorySpecsEditor
                  key={`specs-${selected.id}-${selected.rowVersion}`} category={selected} effective={effectiveSpecs(tree, selected.id)}
                  canManage={canManage} onSaved={() => reload()} onStale={reload}
                />
              </>
            ) : (
              <p className="m-0 text-sm text-ink-muted">{t("empty")}</p>
            )}
          </section>
        </div>
      )}
    </>
  );
}
