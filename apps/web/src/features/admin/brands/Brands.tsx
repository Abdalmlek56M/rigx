"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad } from "@/lib/hooks/use-load";
import { PlusIcon, RefreshIcon } from "@/components/ui/icons";
import { fetchCategories } from "../categories/categories-api";
import { Button } from "../kit/Button";
import { PageHeader } from "../kit/PageHeader";
import { inOrder } from "../kit/reorder-rules";
import { useToast } from "../kit/Toast";
import { useReveal } from "../kit/use-reveal";
import { usePermissions } from "../use-permissions";
import { BrandForm } from "./BrandForm";
import { BrandList } from "./BrandList";
import { BrandLogoCard } from "./BrandLogoCard";
import { filterBrands } from "./brand-rules";
import { fetchBrands, reorderBrands, type Brand } from "./brands-api";

/** ADM-Brands: the brand list (with a search, a hand order and the logos) on the start side and the edit / create panel on the end side. */
export function Brands() {
  const t = useTranslations("admin.brands");
  const { has } = usePermissions();
  const toast = useToast();
  const canManage = has("Catalog.ManageTaxonomy");
  const { state, reload } = useLoad(useCallback(() => fetchBrands(), []));
  const categories = useLoad(useCallback(() => fetchCategories(), []));
  const [pending, setPending] = useState<{ ids: string[]; over: readonly Brand[] } | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [query, setQuery] = useState("");

  const loaded = state.status === "ready" ? state.data : [];
  // The new order shows at once and is dropped as soon as the list is read again (a different array).
  const brands = pending && pending.over === loaded ? inOrder(loaded, pending.ids) : loaded;
  const shown = filterBrands(brands, query);
  const selected = creating ? null : brands.find((brand) => brand.id === selectedId) ?? null;
  const panelRef = useReveal<HTMLElement>(creating ? "new" : selectedId);
  const tree = categories.state.status === "ready" ? categories.state.data : [];
  const orderable = canManage && query.trim() === "";

  async function reorder(ids: string[]) {
    setPending({ ids, over: loaded });
    try {
      await reorderBrands(ids);
      toast(t("reordered"));
    } catch {
      toast(t("reorderFailed"), "error");
    }
    reload();
  }

  const saved = (brand: Brand) => {
    setCreating(false);
    setSelectedId(brand.id);
    reload();
  };

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={canManage ? (
          <Button variant="primary" icon={PlusIcon} onClick={() => { setCreating(true); setSelectedId(null); }}>{t("new")}</Button>
        ) : null}
      />
      {state.status === "error" ? (
        <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
          <p className="m-0 font-bold">{t("failed")}</p>
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[360px_minmax(0,1fr)] lg:items-start">
          <div className="flex flex-col gap-2 rounded-card border border-line bg-surface p-3">
            <label htmlFor="brand-search" className="sr-only">{t("search")}</label>
            <input
              id="brand-search" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={t("search")}
              autoComplete="off" className="h-11 w-full rounded-control border border-input-line bg-surface px-3 text-sm"
            />
            {state.status === "loading" ? <div aria-hidden="true" className="h-64 animate-pulse rounded-control bg-paper" /> : shown.length === 0 ? (
              <p className="m-0 p-3 text-sm text-ink-muted">{brands.length === 0 ? t("emptyList") : t("noMatch")}</p>
            ) : (
              <BrandList
                brands={shown} selectedId={selected?.id ?? null} onSelect={(id) => { setSelectedId(id); setCreating(false); }}
                onReorder={orderable ? (ids) => void reorder(ids) : undefined}
              />
            )}
            {orderable && shown.length > 1 ? <p className="m-0 px-1 text-xs text-ink-muted">{t("orderHint")}</p> : null}
          </div>

          <section ref={panelRef} className="scroll-mt-20 flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
            {creating ? (
              <>
                <h2 className="m-0 text-[17px] font-bold">{t("createTitle")}</h2>
                <BrandForm key="new" brand={null} tree={tree} canManage={canManage} onSaved={saved} onStale={reload} onCancel={() => setCreating(false)} />
              </>
            ) : selected ? (
              <>
                <h2 className="m-0 text-[17px] font-bold">{t("editTitle", { name: selected.name })}</h2>
                <BrandForm
                  key={`${selected.id}-${selected.rowVersion}`} brand={selected} tree={tree} canManage={canManage}
                  onSaved={saved} onStale={reload} onCancel={() => setSelectedId(null)}
                />
                <BrandLogoCard brand={selected} onChanged={() => reload()} />
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
