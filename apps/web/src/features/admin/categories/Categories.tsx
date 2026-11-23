"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useReveal } from "../kit/use-reveal";
import { usePermissions } from "../use-permissions";
import { CategoryForm } from "./CategoryForm";
import { CategoryImageCard } from "./CategoryImageCard";
import { CategorySpecsEditor } from "./CategorySpecsEditor";
import { CategoryTree } from "./CategoryTree";
import { effectiveSpecs, findPath, reorderChildren } from "./category-rules";
import { fetchCategories, reorderCategories, type Category } from "./categories-api";
import { PlusIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button } from "../kit/Button";

/** ADM-Categories: the category tree on the start side and the edit / create panel on the end side. */
export function Categories() {
  const t = useTranslations("admin.categories");
  const { has } = usePermissions();
  const toast = useToast();
  const canManage = has("Catalog.ManageTaxonomy");
  const load = useCallback(() => fetchCategories(), []);
  const { state, reload } = useLoad(load);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [creating, setCreating] = useState<{ parentId: string | null } | null>(null);
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(new Set());
  const [pending, setPending] = useState<{ over: readonly Category[]; next: Category[] } | null>(null);

  const loaded = state.status === "ready" ? state.data : [];
  // The new order shows at once and is dropped as soon as the tree is read again (a different array).
  const tree = pending && pending.over === loaded ? pending.next : loaded;
  const activeId = creating ? null : selectedId ?? tree[0]?.id ?? null;
  const selected = findPath(tree, activeId ?? "").at(-1) ?? null;
  const panelRef = useReveal<HTMLElement>(creating ? `new-${creating.parentId ?? "root"}` : selectedId);

  const toggle = (id: string) => setCollapsed((current) => {
    const next = new Set(current);
    if (!next.delete(id)) next.add(id);
    return next;
  });
  async function reorder(parentId: string | null, ids: string[]) {
    setPending({ over: loaded, next: reorderChildren(loaded, parentId, ids) });
    try {
      await reorderCategories(parentId, ids);
      toast(t("reordered"));
    } catch {
      toast(t("reorderFailed"), "error");
    }
    reload();
  }

  const saved = (category: Category) => {
    setCreating(null);
    setSelectedId(category.id);
    reload();
  };

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={canManage ? (
          <Button variant="primary" icon={PlusIcon} onClick={() => { setCreating({ parentId: null }); setSelectedId(null); }}>
            {t("newRoot")}
          </Button>
        ) : null}
      />
      {state.status === "error" ? (
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
      ) : (
        <div className="grid gap-4 lg:grid-cols-[360px_minmax(0,1fr)] lg:items-start">
          <div className="flex flex-col gap-2">
            {state.status === "loading" ? <LoadingCard className="h-64" /> : (
              <CategoryTree nodes={tree} selectedId={activeId} collapsed={collapsed} onSelect={(id) => { setSelectedId(id); setCreating(null); }} onToggle={toggle} onReorder={canManage ? (parentId, ids) => void reorder(parentId, ids) : undefined} />
            )}
            {canManage && tree.length > 1 ? <p className="m-0 px-1 text-xs text-ink-muted">{t("orderHint")}</p> : null}
          </div>

          <section ref={panelRef} className={cardClass({ className: "scroll-mt-20 gap-4" })}>
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
                    <Button icon={PlusIcon} onClick={() => setCreating({ parentId: selected.id })}>
                      {t("newChild")}
                    </Button>
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
